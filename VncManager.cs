using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using MarcusW.VncClient.Protocol.Implementation.Services.Transports;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Rendering;
using MarcusW.VncClient.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Size = MarcusW.VncClient.Size;

namespace DisControl; 

public static class VncManager {
    private class NoPasswordAuth : IAuthenticationHandler {
        public Task<TInput> ProvideAuthenticationInputAsync<TInput>(RfbConnection connection, 
            ISecurityType securityType, IAuthenticationInputRequest<TInput> request)
            where TInput : class, IAuthenticationInput {
            if (typeof(TInput) == typeof(PasswordAuthenticationInput))
                throw new InvalidOperationException("The authentication input request is not supported by this authentication handler.");
            
            return Task.FromResult((TInput)Convert.ChangeType(new PasswordAuthenticationInput(""), typeof(TInput)));
        }
    }
    
    private sealed class FramebufferReference : IFramebufferReference {
        public PixelFormat Format => PixelFormat.Plain;
        public double HorizontalDpi => _size.Width;
        public double VerticalDpi => _size.Height;
        public nint Address => _address;
        public Size Size => _size;
        private nint _address;
        private Size _size;

        internal unsafe FramebufferReference(byte[] bitmap, Size size) {
            _address = (nint)bitmap.AsMemory().Pin().Pointer;
            _size = size;
        }

        public void Dispose() { }
    }
    
    private class RenderTarget : IRenderTarget {
        public volatile byte[]? Bitmap;
        public Size Size;

        public IFramebufferReference GrabFramebufferReference(Size size, IImmutableSet<Screen> layout) {
            byte[]? bitmap; Size = size;
            if (Bitmap == null || Size != size) {
                bitmap = new byte[size.Width * size.Height * Unsafe.SizeOf<Abgr32>()];
                Bitmap = bitmap;
            }

            return new FramebufferReference(Bitmap, size);
        }
    }
    
    private static VncClient _client = new(new NullLoggerFactory());
    private static RenderTarget _target = new();
    private static RfbConnection? _connection;
    private static Position? _lastPos;

    public static async Task Connect() {
        _connection = await _client.ConnectAsync(new ConnectParameters {
            ConnectTimeout = TimeSpan.FromSeconds(5),
            MaxReconnectAttempts = 5,
            AuthenticationHandler = new NoPasswordAuth(),
            TransportParameters = new TcpTransportParameters {
                Host = "127.0.0.1", Port = 5901
            }});
        _connection.RenderTarget = _target;
    }

    public static bool IsConnected()
        => _connection is { ConnectionState: ConnectionState.Connected };
    
    public static async Task<Stream> GetScreenPNG() {
        var stream = new MemoryStream();
        using (var image = Image.LoadPixelData<Abgr32>(_target.Bitmap,
                   _target.Size.Width, _target.Size.Height))
            await image.SaveAsPngAsync(stream);
        stream.Position = 0;
        return stream;
    }

    public static async Task SendKeyCombo(KeySymbol[] keys) {
        foreach (var i in keys)
            await _connection!.SendMessageAsync(new KeyEventMessage(true, i));
        foreach (var i in keys)
            await _connection!.SendMessageAsync(new KeyEventMessage(false, i));
    }

    public static async Task SendKey(KeySymbol key) {
        await _connection!.SendMessageAsync(new KeyEventMessage(true, key));
        await _connection.SendMessageAsync(new KeyEventMessage(false, key));
    }

    public static async Task Type(string text) {
        var enumKeys = text.ToArray().Select(UniKey.FromUnicode);
        foreach (var i in enumKeys) {
            await _connection!.SendMessageAsync(new KeyEventMessage(true, i));
            await _connection.SendMessageAsync(new KeyEventMessage(false, i));
        }
    }

    public static async Task SendMouse(int x, int y, MouseButtons button, bool relative = false) {
        switch (relative) {
            case true when !_lastPos.HasValue:
                throw new InvalidOperationException("Perform an absolute mouse action first!");
            case true: {
                var pos = new Position(_lastPos!.Value.X + x, _lastPos.Value.Y + y);
                await _connection!.SendMessageAsync(new PointerEventMessage(pos, button));
                _lastPos = pos; break;
            }
            case false: {
                var pos = new Position(x, y);
                await _connection!.SendMessageAsync(new PointerEventMessage(pos, button));
                _lastPos = pos; break;
            }
        }
    }
}