﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Platibus.Http.Models;
using Platibus.Journaling;
using Platibus.Security;
using Platibus.Serialization;

namespace Platibus.Http.Controllers
{
    /// <summary>
    /// An HTTP resource controller for querying the message journal
    /// </summary>
    public class JournalController : IHttpResourceController
    {
        private readonly NewtonsoftJsonSerializer _serializer = new NewtonsoftJsonSerializer();
        private readonly IAuthorizationService _authorizationService;
        private readonly IMessageJournal _messageJournal;
        
        /// <summary>
        /// Initializes a <see cref="JournalController"/> with the specified 
        /// <paramref name="messageJournal"/>
        /// </summary>
        /// <param name="messageJournal">The message journal</param>
        /// <param name="authorizationService">(Optional) Used to determine whether a requestor is 
        /// authorized to query the message journal</param>
        public JournalController(IMessageJournal messageJournal, IAuthorizationService authorizationService = null)
        {
            if (messageJournal == null) throw new ArgumentNullException("messageJournal");
            _messageJournal = messageJournal;
            _authorizationService = authorizationService;
        }

        /// <inheritdoc />
        public async Task Process(IHttpResourceRequest request, IHttpResourceResponse response, IEnumerable<string> subPath)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (response == null) throw new ArgumentNullException("response");

            if (!request.IsGet())
            {
                response.StatusCode = 405;
                response.AddHeader("Allow", "GET");
                return;
            }

            await Get(request, response);
        }

        private async Task Get(IHttpResourceRequest request, IHttpResourceResponse response)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (response == null) throw new ArgumentNullException("response");

            var authorized = _authorizationService == null ||
                             await _authorizationService.IsAuthorizedToQueryJournal(request.Principal);

            if (!authorized)
            {
                response.StatusCode = 401;
                response.StatusDescription = "Unauthorized";
                return;
            }

            var responseModel = new JournalGetResponseModel();
            var start = await GetStartPosition(request, responseModel.Errors);
            var count = GetCount(request, responseModel.Errors);
            var filter = ConfigureFilter(request, responseModel.Errors);
            
            if (responseModel.Errors.Any())
            {
                response.StatusCode = 400;
            }
            else
            {
                var result = await _messageJournal.Read(start, count, filter);
                responseModel.Start = start.ToString();
                responseModel.Next = result.Next.ToString();
                responseModel.EndOfJournal = result.EndOfJournal;
                responseModel.Entries = result.Entries.Select(jm => new MessageJournalEntryModel
                {
                    Position = jm.Position.ToString(),
                    Category = jm.Category,
                    Timestamp = jm.Timestamp,
                    Data = new MessageJournalEntryDataModel
                    {
                        Headers = jm.Data.Headers.ToDictionary(h => (string) h.Key, h => h.Value),
                        Content = jm.Data.Content
                    }
                }).ToList();
                response.StatusCode = 200;
            }
            
            response.ContentType = "application/json";
            var serializedContent = _serializer.Serialize(responseModel);
            var encoding = response.ContentEncoding;
            var encodedContent = encoding.GetBytes(serializedContent);
            await response.OutputStream.WriteAsync(encodedContent, 0, encodedContent.Length);
        }

        private async Task<MessageJournalPosition> GetStartPosition(IHttpResourceRequest request, ICollection<ErrorModel> errors)
        {
            var startStr = request.QueryString["start"];
            try
            {
                return string.IsNullOrWhiteSpace(startStr)
                    ? await _messageJournal.GetBeginningOfJournal()
                    : _messageJournal.ParsePosition(startStr);
            }
            catch (Exception)
            {
                errors.Add(new ErrorModel("Invalid start position", "start"));
                return null;
            }
        }

        private static int GetCount(IHttpResourceRequest request, ICollection<ErrorModel> errors)
        {
            var countStr = request.QueryString["count"];
            if (string.IsNullOrWhiteSpace(countStr))
            {
                errors.Add(new ErrorModel("Count is required", "count"));
                return 0;
            }

            int count;
            if (!int.TryParse(countStr, out count) || count <= 0)
            {
                errors.Add(new ErrorModel("Count must be a positive integer value", "count"));
                return 0;
            }

            return count;
        }

        private static MessageJournalFilter ConfigureFilter(IHttpResourceRequest request, ICollection<ErrorModel> errors)
        {
            var filter = new MessageJournalFilter();
            var topic = request.QueryString["topic"];
            if (!string.IsNullOrWhiteSpace(topic))
            {
                filter.Topics = topic.Split(',')
                    .Select(t => (TopicName)t)
                    .ToList();
            }

            var category = request.QueryString["category"];
            if (!string.IsNullOrWhiteSpace(category))
            {
                filter.Categories = category.Split(',')
                    .Select(t => (JournaledMessageCategory)t.Trim())
                    .ToList();
            }

            return filter;
        }
    }
}