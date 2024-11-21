using System.Net.Sockets;
using System.Net;
using System.Text;

public class Server
{
    public static void Main()
    {
        int port = 6881;
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"Server started on port {port}");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("Client connected.");

            NetworkStream stream = client.GetStream();

            try
            {
                byte[] buffer = new byte[68];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 68)
                {
                    // Handshake OK
                    Console.WriteLine("Received Handshake.");
                    string protocol = Encoding.ASCII.GetString(buffer, 1, 19).Trim('\0');
                    Console.WriteLine($"Protocol: {protocol}");

                    byte[] reserved = new byte[8];
                    Array.Copy(buffer, 20, reserved, 0, 8);
                    Console.WriteLine($"Reserved: {BitConverter.ToString(reserved)}");

                    byte[] infoHash = new byte[20];
                    Array.Copy(buffer, 28, infoHash, 0, 20);
                    Console.WriteLine($"InfoHash: {BitConverter.ToString(infoHash)}");

                    byte[] peerID = new byte[20];
                    Array.Copy(buffer, 48, peerID, 0, 20);
                    Console.WriteLine($"PeerID: {Encoding.ASCII.GetString(peerID)}");

                    // Respond to handshake
                    byte[] response = new byte[68];
                    response[0] = 19; // Protocol length
                    Encoding.ASCII.GetBytes("BitTorrent protocol").CopyTo(response, 1);
                    reserved.CopyTo(response, 20); // Copy reserved bytes
                    infoHash.CopyTo(response, 28); // Echo InfoHash
                    Encoding.ASCII.GetBytes("-SERVER-1234567890-").CopyTo(response, 48);

                    stream.Write(response, 0, response.Length);
                    Console.WriteLine("Sent Handshake Response.");

                    
                    byte[] bitfieldMessage = ReadBitfield(stream); 
                    if (bitfieldMessage != null)
                    {
                        HandleBitfieldMessage(bitfieldMessage); 
                    }
                    else
                    {
                        Console.WriteLine("No bitfield received!");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid handshake received.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing handshake: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }
    }



    // Bitfield OK
    public static byte[] ReadBitfield(NetworkStream stream)
    {
        try
        {
            
            byte[] lengthByte = new byte[1];
            int bytesRead = stream.Read(lengthByte, 0, 1);

            if (bytesRead == 1)
            {
                int messageLength = lengthByte[0];
                Console.WriteLine($"Message length for Bitfield: {messageLength}");

                if (messageLength > 0)
                {
                    byte[] bitfieldMessage = new byte[messageLength];
                    bytesRead = stream.Read(bitfieldMessage, 0, messageLength);

                    if (bytesRead == messageLength)
                    {
                        Console.WriteLine("Bitfield successfully received.");
                        return bitfieldMessage;
                    }
                    else
                    {
                        Console.WriteLine("Failed to read full bitfield.");
                    }
                }
            }
            else
            {
                Console.WriteLine("No length byte received.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading bitfield: {ex.Message}");
        }

        return null;
    }

    public static void HandleBitfieldMessage(byte[] bitfieldMessage)
    {
        if (bitfieldMessage == null || bitfieldMessage.Length == 0)
        {
            Console.WriteLine("Bitfield is empty !.");
            return;
        }

        int totalPieces = bitfieldMessage.Length * 8; // Calcul du nombre total de pièces
        Console.WriteLine($"Total {totalPieces} pieces to analyze.");

        
        Console.WriteLine("Raw bitfield (hex) :");
        Console.WriteLine(BitConverter.ToString(bitfieldMessage));

       
        for (int byteIndex = 0; byteIndex < bitfieldMessage.Length; byteIndex++)
        {
            byte currentByte = bitfieldMessage[byteIndex];
            for (int bitIndex = 7; bitIndex >= 0; bitIndex--)
            {
                int bitPosition = byteIndex * 8 + (7 - bitIndex);
                if (bitPosition >= totalPieces)
                    break;

                bool isPieceOwned = (currentByte & (1 << bitIndex)) != 0;
                string ownership = isPieceOwned ? "Owned" : "Not owned";

                Console.WriteLine($"Piece {bitPosition + 1}: {ownership} (Byte: {byteIndex}, Bit: {bitIndex})");
            }
        }
    }
}
