﻿#region Licence

/* The MIT License (MIT)
Copyright © 2022 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the “Software”), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion

using System;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SecurityToken;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Paramore.Brighter.MessagingGateway.AWSSQS;

namespace Paramore.Brighter.AWS.Tests.Helpers;

internal class AWSClientFactory
{
    private readonly AWSCredentials _credentials;
    private readonly RegionEndpoint _region;
    private readonly Action<ClientConfig>? _clientConfigAction;

    public AWSClientFactory(AWSMessagingGatewayConnection connection)
    {
        _credentials = connection.Credentials;
        _region = connection.Region;
        _clientConfigAction = connection.ClientConfigAction;
    }

    public AWSClientFactory(AWSCredentials credentials, RegionEndpoint region, Action<ClientConfig>? clientConfigAction)
    {
        _credentials = credentials;
        _region = region;
        _clientConfigAction = clientConfigAction;
    }

    public AmazonSimpleNotificationServiceClient CreateSnsClient()
    {
        var config = new AmazonSimpleNotificationServiceConfig { RegionEndpoint = _region };

        if (_clientConfigAction != null)
        {
            _clientConfigAction(config);
        }

        return new AmazonSimpleNotificationServiceClient(_credentials, config);
    }

    public AmazonSQSClient CreateSqsClient()
    {
        var config = new AmazonSQSConfig { RegionEndpoint = _region };

        if (_clientConfigAction != null)
        {
            _clientConfigAction(config);
        }

        return new AmazonSQSClient(_credentials, config);
    }

    public AmazonSecurityTokenServiceClient CreateStsClient()
    {
        var config = new AmazonSecurityTokenServiceConfig { RegionEndpoint = _region };

        if (_clientConfigAction != null)
        {
            _clientConfigAction(config);
        }

        return new AmazonSecurityTokenServiceClient(_credentials, config);
    }

    public AmazonS3Client CreateS3Client()
    {
        var config = new AmazonS3Config { RegionEndpoint = _region };

        if (_clientConfigAction != null)
        {
            _clientConfigAction(config);
        }

        return new AmazonS3Client(_credentials, config);
    }
}