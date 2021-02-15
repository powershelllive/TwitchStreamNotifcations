using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Markekraus.TwitchStreamNotifications.Models;
using System.Collections.Generic;

namespace Markekraus.TwitchStreamNotifications
{
    public static class Utility
    {
        public const string NameNullString = "--";
        public const string DISABLE_NOTIFICATIONS = "DISABLE_NOTIFICATIONS";
        public const string ApplicationJsonContentType = "application/json";

        public readonly static Dictionary<TwitchScheduledChannelEventType, string> TypeStringLookup = new Dictionary<TwitchScheduledChannelEventType, string>(){
            {TwitchScheduledChannelEventType.Unknown, "unknown"},
            {TwitchScheduledChannelEventType.Hour, "an hour"},
            {TwitchScheduledChannelEventType.Day, "a day"},
            {TwitchScheduledChannelEventType.Week, "a week"}
        };

        public static async Task<byte[]> ComputeRequestBodySha256HashAsync(
            HttpRequest request,
            string secret)
        {
            await PrepareRequestBody(request);
            var secretBytes = Encoding.UTF8.GetBytes(secret);

            using (HMACSHA256 hasher = new HMACSHA256(secretBytes))
            {
                try
                {
                    Stream inputStream = request.Body;

                    int bytesRead;
                    byte[] buffer = new byte[4096];

                    while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        hasher.TransformBlock(buffer, inputOffset: 0, inputCount: bytesRead,
                            outputBuffer: null, outputOffset: 0);
                    }

                    hasher.TransformFinalBlock(Array.Empty<byte>(), inputOffset: 0, inputCount: 0);

                    return hasher.Hash;
                }
                finally
                {
                    request.Body.Seek(0L, SeekOrigin.Begin);
                }
            }
        }

        public static async Task PrepareRequestBody(HttpRequest request)
        {
            if (!request.Body.CanSeek)
            {
                request.EnableBuffering();
                await StreamHelperExtensions.DrainAsync(request.Body, CancellationToken.None);
            }

            request.Body.Seek(0L, SeekOrigin.Begin);
        }

        public static byte[] FromHex(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return Array.Empty<byte>();
            }

            try
            {
                var data = new byte[content.Length / 2];
                var input = 0;
                for (var output = 0; output < data.Length; output++)
                {
                    data[output] = Convert.ToByte(new string(new char[2] { content[input++], content[input++] }), 16);
                }

                if (input != content.Length)
                {
                    return null;
                }

                return data;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is FormatException)
            {
                return null;
            }
        }

        public static bool SecretEqual(byte[] inputA, byte[] inputB)
        {
            if (ReferenceEquals(inputA, inputB))
            {
                return true;
            }

            if (inputA == null || inputB == null || inputA.Length != inputB.Length)
            {
                return false;
            }

            var areSame = true;
            for (var i = 0; i < inputA.Length; i++)
            {
                areSame &= inputA[i] == inputB[i];
            }

            return areSame;
        }
    }
}