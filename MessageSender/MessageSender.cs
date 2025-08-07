using System.Net.Sockets;

/// <summary>
/// MessageSender - Modified to fix Big Endian compliance issue
/// 
/// MODIFICATIONS MADE:
/// - Original code used BinaryWriter which writes multi-byte values in Little Endian format
/// - Protocol specification requires Big Endian format for all multi-byte fields
/// - Changed from BinaryWriter to manual byte writing to ensure Big Endian compliance
/// - Added console output for better testing visibility
/// - Added stream.Flush() to ensure immediate data transmission
/// 
/// These modifications were made as permitted by the assignment instructions:
/// "feel free to modify it if needed"
/// </summary>
class MessageSender
{
    public static void SendMessage(Uri address, byte[] deviceId, ushort messageCounter, byte messageType, byte[] payload)
    {
        ushort payloadLength = (ushort)payload.Length;
        using var client = new TcpClient(address.Host, address.Port);
        using var stream = client.GetStream();

        // Write sync word in Big Endian (0xAA55)
        stream.WriteByte(0xAA);
        stream.WriteByte(0x55);

        // Write device ID (4 bytes, already in correct order)
        stream.Write(deviceId);

        // Write message counter in Big Endian (2 bytes)
        stream.WriteByte((byte)(messageCounter >> 8));
        stream.WriteByte((byte)(messageCounter & 0xFF));

        // Write message type (1 byte)
        stream.WriteByte(messageType);

        // Write payload length in Big Endian (2 bytes)
        stream.WriteByte((byte)(payloadLength >> 8));
        stream.WriteByte((byte)(payloadLength & 0xFF));

        // Write payload (raw bytes)
        stream.Write(payload);

        stream.Flush();
    }

    public static void SendStream(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid address format. Use 'hostname:port'.", nameof(address));
        }
        SendStream(uri);
    }

    public static void SendStream(Uri address)
    {
        byte[] firstDeviceId = [0x01, 0x02, 0x03, 0x04];
        byte[] secondDeviceId = [0x05, 0x06, 0x07, 0x08];

        Console.WriteLine("Sending messages...");
        SendMessage(address, firstDeviceId, 1, 0x01, [0x01, 0x02, 0x03]);
        Console.WriteLine("Sent message 1");

        SendMessage(address, firstDeviceId, 1, 0x01, [0x01, 0x02, 0x03]);
        Console.WriteLine("Sent message 2 (duplicate)");

        SendMessage(address, firstDeviceId, 1, 0x02, [0x07, 0x08, 0x09]);
        Console.WriteLine("Sent message 3");

        SendMessage(address, secondDeviceId, 1, 0x01, [0x04, 0x05, 0x06]);
        Console.WriteLine("Sent message 4");

        Console.WriteLine("All messages sent!");
    }
}