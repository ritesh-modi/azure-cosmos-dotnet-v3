﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.Azure.Documents.Collections;

namespace Microsoft.Azure.Cosmos
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Net;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Cosmos.Tests;

    /// <summary>
    /// Tests for <see cref="GatewayAccountReader"/>.
    /// </summary>
    [TestClass]
    public class GatewayAccountReaderTests
    {
        [TestMethod]
        public async Task GatewayAccountReader_MessageHandler()
        {
            HttpMessageHandler messageHandler = new CustomMessageHandler();
            HttpClient staticHttpClient = new HttpClient(messageHandler);

            GatewayAccountReader accountReader = new GatewayAccountReader(
                new Uri("https://localhost"),
                Mock.Of<IComputeHash>(),
                false,
                null,
                new ConnectionPolicy(),
                MockCosmosUtil.CreateCosmosHttpClient(() => staticHttpClient));

            DocumentClientException exception = await Assert.ThrowsExceptionAsync<DocumentClientException>(() => accountReader.InitializeReaderAsync());
            Assert.AreEqual(HttpStatusCode.Conflict, exception.StatusCode);
        }

        [TestMethod]
        public async Task DocumentClient_BuildHttpClientFactory_WithHandler()
        {
            HttpMessageHandler messageHandler = new CustomMessageHandler();
            ConnectionPolicy connectionPolicy = new ConnectionPolicy()
            {
                HttpClientFactory = () => new HttpClient(messageHandler)
            };

            CosmosHttpClient httpClient = CosmosHttpClientCore.CreateWithConnectionPolicy(
                apiType: ApiType.None,
                eventSource: DocumentClientEventSource.Instance,
                connectionPolicy: connectionPolicy,
                httpMessageHandler: null,
                sendingRequestEventArgs: null,
                receivedResponseEventArgs: null);

            Assert.IsNotNull(httpClient);
            HttpResponseMessage response = await httpClient.GetAsync(
                uri: new Uri("https://localhost"),
                additionalHeaders: new DictionaryNameValueCollection(),
                resourceType: ResourceType.Document,
                diagnosticsContext: null,
                cancellationToken: default);

            Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        }

        [TestMethod]
        public void DocumentClient_BuildHttpClientFactory_WithFactory()
        {
            HttpClient staticHttpClient = new HttpClient();

            Mock<Func<HttpClient>> mockFactory = new Mock<Func<HttpClient>>();
            mockFactory.Setup(f => f()).Returns(staticHttpClient);

            ConnectionPolicy connectionPolicy = new ConnectionPolicy()
            {
                HttpClientFactory = mockFactory.Object
            };

            CosmosHttpClient httpClient = CosmosHttpClientCore.CreateWithConnectionPolicy(
                apiType: ApiType.None,
                eventSource: DocumentClientEventSource.Instance,
                connectionPolicy: connectionPolicy,
                httpMessageHandler: null,
                sendingRequestEventArgs: null,
                receivedResponseEventArgs: null);

            Assert.IsNotNull(httpClient);

            Mock.Get(mockFactory.Object)
                .Verify(f => f(), Times.Once);
        }

        public class CustomMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict) { RequestMessage = request, Content = new StringContent("Notfound") });
            }
        }
    }
}
