﻿// The MIT License (MIT)
// 
// Copyright (c) 2014 Jesse Sweetland
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace Pluribus
{
    public class MemoryCacheReplyHub : IDisposable
    {
        private bool _disposed;
        private readonly MemoryCache _cache = new MemoryCache("MemoryCacheReplyHub");
        private readonly TimeSpan _replyTimeout;

        public MemoryCacheReplyHub(TimeSpan replyTimeout)
        {
            _replyTimeout = (replyTimeout <= TimeSpan.Zero) ? TimeSpan.FromMinutes(5) : replyTimeout;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ISentMessage CreateSentMessage(Message message)
        {
            CheckDisposed();
            return new SentMessageWithCachedReplies(this, message.Headers.MessageId);
        }

        public IObservable<Message> ObserveReplies(MessageId relatedToMessageId)
        {
            CheckDisposed();
            var replyStream =
                (ReplyStream)
                    _cache.AddOrGetExisting(relatedToMessageId, new ReplyStream(), DateTime.UtcNow.Add(_replyTimeout));

            if (replyStream == null)
            {
                // MemoryCache.AddOrGetExisting returns null if the key does not
                // already exist, so we have to fetch it in these cases. See:
                // http://msdn.microsoft.com/en-us/library/dd988741%28v=vs.110%29.aspx
                replyStream = (ReplyStream) _cache[relatedToMessageId];
            }
            return replyStream;
        }

        public Task ReplyReceived(Message replyMessage, bool lastReply)
        {
            CheckDisposed();
            return Task.Run(() =>
            {
                var relatedToMessageId = replyMessage.Headers.RelatedTo;
                var replyStream = _cache.Get(relatedToMessageId) as ReplyStream;
                if (replyStream == null) return;

                replyStream.NotifyReplyReceived(replyMessage);
                if (lastReply)
                {
                    replyStream.NotifyCompleted();
                }
            });
        }

        private void CheckDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
        }

        ~MemoryCacheReplyHub()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _cache.Dispose();
            }
            _disposed = true;
        }
    }
}