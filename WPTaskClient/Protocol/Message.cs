using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace WPTaskClient.Protocol
{
    class Message
    {
        private IReadOnlyDictionary<string, string> header;
        private string body;

        public IReadOnlyDictionary<string, string> Header { get => header; }
        public string Body => body;

        private Message() { }

        public Message(IReadOnlyDictionary<string, string> header, string body)
        {
            this.header = header.ToDictionary(entry => entry.Key, entry => entry.Value);
            this.body = body;
        }

        public static async Task<Message> FromStream(Stream inputStream)
        {
            var buffer = new byte[4];
            var numRead = await inputStream.ReadAsync(buffer, 0, 4);
            if (numRead != 4)
            {
                throw new EndOfStreamException();
            }
            var numBytes = buffer[0] << 0x18 | buffer[1] << 0x10 | buffer[2] << 0x08 | buffer[3];
            numBytes -= 4; // subtract size bytes

            buffer = new byte[numBytes];
            numRead = 0;
            var numRemaining = numBytes;
            // we may not read the whole message at once
            while (numRemaining > 0)
            {
                var numReadNow = await inputStream.ReadAsync(buffer, numRead, numRemaining);
                if (numReadNow == 0)
                {
                    throw new EndOfStreamException();
                }
                numRead += numReadNow;
                numRemaining -= numReadNow;
            }
            if (numRead < numBytes)
            {
                throw new EndOfStreamException();
            }
            var message = Encoding.UTF8.GetString(buffer);
            var headerEnd = message.IndexOf("\n\n");
            var headerString = headerEnd == -1 ? message : message.Substring(0, headerEnd);
            var body = headerEnd == -1 ? "" : message.Substring(headerEnd + 2);
            var header = headerString.Split('\n').Select(line =>
            {
                var pos = line.IndexOf(": ");
                if (pos == -1)
                {
                    throw new FormatException("header line missing ': ' separator");
                }
                return new { first = line.Substring(0, pos), second = line.Substring(pos + 2) };
            }).ToDictionary(pair => pair.first, pair => pair.second);
            return new Message
            {
                header = header,
                body = body,
            };
        }

        public async Task ToStream(Stream outputStream)
        {
            var message = string.Join("\n", header.Select(entry => entry.Key + ": " + entry.Value)) + "\n\n" + body;
            
            var len = Encoding.UTF8.GetByteCount(message) + 4;
            var buffer = new byte[len];
            buffer[0] = (byte)((len >> 0x18) & 0xFF);
            buffer[1] = (byte)((len >> 0x10) & 0xFF);
            buffer[2] = (byte)((len >> 0x08) & 0xFF);
            buffer[3] = (byte)((len >> 0x00) & 0xFF);
            Encoding.UTF8.GetBytes(message, 0, message.Length, buffer, 4);
            await outputStream.WriteAsync(buffer, 0, len);
            await outputStream.FlushAsync();
        }
    }
}
