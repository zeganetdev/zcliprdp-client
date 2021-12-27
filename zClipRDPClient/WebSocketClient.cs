
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static zRDPClip.Delegate;

namespace zRDPClip
{
    public class WebSocketClient
    {
        public IODelegate Input;
        public MessageDelegate Message;
        private SocketIO Client { get; set; }
        public string Url { get; set; }
        public string Usuario { get; set; }

        public WebSocketClient(ClipManager clipManager)
        {
            Input = clipManager.PrintInput;
            Message = clipManager.MessageOuput;
        }

        public void Initialize()
        {
            Client = new SocketIO(Url);

            Client.On("messagePrivate", response =>
            {
                Console.WriteLine(response);
                var clip = response.GetValue<ClipDTO>();
                Input(clip.Format, clip.Data);
                clip = null;
            });

            Client.OnConnected += async (sender, e) =>
            {
                Message("", $"OnConnected {e}");
                await Client.EmitAsync($"addUser", new { UserName = Usuario });
            };

            Client.OnDisconnected += (sender, e) =>
            {
                Message("", $"OnDisconnected {e}");
            };

            Client.OnError += (sender, e) =>
            {
                Message("", $"OnError {e}");
            };

            Client.OnReconnectAttempt += (sender, e) =>
            {
                Message("", $"OnReconnectAttempt {e}");
            };

            Client.OnReconnected += (sender, e) =>
            {
                Message("", $"OnReconnected {e}");
            };

            Client.OnReconnectError += (sender, e) =>
            {
                Message("", $"OnReconnectError {e}");
            };

            Client.OnReconnectFailed += (sender, e) =>
            {
                Message("", $"OnReconnectFailed {e}");
            };
        }

        public async Task DisconnectAsync()
        {
            await Client.DisconnectAsync();
        }

        public async Task ConnectAsync()
        {
            await Client.ConnectAsync();
        }

        public async Task SendMessageAsync(ClipDTO clipDTO)
        {
            await Client.EmitAsync($"messagePrivate", clipDTO);
        }

    }

    public class ClipDTO
    {
        public string UserName { get; set; }
        public string Format { get; set; }
        public object Data { get; set; }
    }

}
