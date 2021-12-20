
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
        public static IODelegate Input;
        private SocketIO Client { get; set; }
        public string Url { get; set; }
        public string Usuario { get; set; }

        public WebSocketClient()
        {
            Input = ClipManager.PrintInput;
        }

        public void Initialize()
        {
            Client = new SocketIO(Url);

            Client.On("messagePrivate", response =>
            {
                Console.WriteLine(response);
                var clip = response.GetValue<ClipDTO>();
                Input(clip.Format, clip.Data);
            });

            Client.OnConnected += async (sender, e) =>
            {
                await Client.EmitAsync($"addUser", new { UserName = Usuario });
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
