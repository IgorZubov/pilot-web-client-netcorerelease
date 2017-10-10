﻿using Ascon.Pilot.Core;
using Ascon.Pilot.Server.Api;
using Ascon.Pilot.Server.Api.Contracts;
using Ascon.Pilot.WebClient.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace Ascon.Pilot.WebClient.Models
{
    public interface IContext : IDisposable
    {
        void Build(HttpContext http);
        IRepository Repository { get; }
        IServerApi ServerApi { get; }
        HttpPilotClient Client { get; }
        DDatabaseInfo Connect(HttpContext http, Credentials credentials);
    }

    class Context : IContext
    {
        private Repository _repository;
        private HttpPilotClient _client;
        private ServerCallback _serverCallback;
        bool _isInitialized;

        public Context()
        {
            _serverCallback = new ServerCallback();
        }

        public DDatabaseInfo Connect(HttpContext http, Credentials credentials)
        {
             _client = new HttpPilotClient();
            _client.Connect(ApplicationConst.PilotServerUrl);
            ServerApi = _client.GetServerApi(_serverCallback);
            var dbInfo = ServerApi.OpenDatabase(credentials.DatabaseName, credentials.Username, credentials.ProtectedPassword, credentials.UseWindowsAuth);
            http.SetClient(_client, credentials.Sid);
            return dbInfo;
        }

        public void Build(HttpContext http)
        {
            if (_isInitialized)
                return;

            ServerApi = http.GetServerApi(_serverCallback);
            _repository = new Repository(ServerApi);
            _serverCallback.SetCallbackListener(_repository);
            var login = http.User.FindFirstValue(ClaimTypes.Name);
            _repository.Initialize(login);
            _isInitialized = true;
        }

        public IRepository Repository
        {
            get { return _repository; }
        }

        public IServerApi ServerApi
        {
            get; private set;
        }

        public HttpPilotClient Client
        {
            get { return _client; }
        }

        public void Dispose()
        {
            _client?.Disconnect();
            _client?.Dispose();
        }
    }
}
