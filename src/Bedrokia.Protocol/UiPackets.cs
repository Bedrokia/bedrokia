using System.Text;

namespace Bedrokia.Protocol;

/// <summary>
/// Packet serialization and parsing for Bedrock Forms UI and Data-Driven UI (DDUI / DataStore).
/// </summary>
public static class UiPackets
{
    /// <summary>
    /// Creates a ModalFormRequestPacket (0x64 / 100) payload.
    /// </summary>
    public static byte[] ModalFormRequest(uint formId, ReadOnlySpan<byte> jsonData)
    {
        var writer = new BedrockWriter();
        writer.VarUInt32(PacketId.ModalFormRequest);
        writer.VarUInt32(formId);
        writer.VarUInt32((uint)jsonData.Length);
        writer.Bytes(jsonData);
        return writer.ToArray();
    }

    /// <summary>
    /// Creates a ModalFormRequestPacket payload from a JSON string.
    /// </summary>
    public static byte[] ModalFormRequest(uint formId, string jsonData) =>
        ModalFormRequest(formId, Encoding.UTF8.GetBytes(jsonData));

    /// <summary>
    /// Decodes a ModalFormResponsePacket (0x65 / 101) sent by the client.
    /// </summary>
    public static (uint FormId, bool HasResponseData, string? ResponseJson, bool HasCancelReason, byte? CancelReason) ReadModalFormResponse(ref BedrockReader reader)
    {
        uint formId = reader.VarUInt32();
        bool hasResponse = reader.Bool();
        string? responseJson = hasResponse ? reader.String() : null;
        bool hasCancel = reader.Bool();
        byte? cancelReason = hasCancel ? reader.Byte() : null;
        return (formId, hasResponse, responseJson, hasCancel, cancelReason);
    }

    /// <summary>
    /// Creates a ServerSettingsResponsePacket (0x67 / 103) payload.
    /// </summary>
    public static byte[] ServerSettingsResponse(uint formId, ReadOnlySpan<byte> jsonData)
    {
        var writer = new BedrockWriter();
        writer.VarUInt32(PacketId.ServerSettingsResponse);
        writer.VarUInt32(formId);
        writer.VarUInt32((uint)jsonData.Length);
        writer.Bytes(jsonData);
        return writer.ToArray();
    }

    /// <summary>
    /// Creates a ClientboundDataDrivenUIShowScreenPacket (320) payload.
    /// </summary>
    public static byte[] DataDrivenUIShowScreen(string screenId, uint formId, string? initialDataStoreJson = null)
    {
        var writer = new BedrockWriter();
        writer.VarUInt32(PacketId.ClientboundDataDrivenUIShowScreen);
        writer.String(screenId);
        writer.VarUInt32(formId);
        writer.Bool(initialDataStoreJson is not null);
        if (initialDataStoreJson is not null)
            writer.String(initialDataStoreJson);
        return writer.ToArray();
    }

    /// <summary>
    /// Creates a ClientboundDataDrivenUICloseAllScreensPacket (321) payload.
    /// </summary>
    public static byte[] DataDrivenUICloseAllScreens()
    {
        var writer = new BedrockWriter();
        writer.VarUInt32(PacketId.ClientboundDataDrivenUICloseAllScreens);
        return writer.ToArray();
    }

    /// <summary>
    /// Creates a ClientboundDataDrivenUIReloadPacket (322) payload.
    /// </summary>
    public static byte[] DataDrivenUIReload()
    {
        var writer = new BedrockWriter();
        writer.VarUInt32(PacketId.ClientboundDataDrivenUIReload);
        return writer.ToArray();
    }

    /// <summary>
    /// Creates a ClientboundDataStorePacket (323) payload to synchronize property updates with client data stores.
    /// </summary>
    public static byte[] ClientboundDataStore(string storeName, IReadOnlyDictionary<string, string> updates)
    {
        var writer = new BedrockWriter();
        writer.VarUInt32(PacketId.ClientboundDataStore);
        writer.String(storeName);
        writer.VarUInt32((uint)updates.Count);
        foreach (var (key, value) in updates)
        {
            writer.String(key);
            writer.String(value);
        }
        return writer.ToArray();
    }

    /// <summary>
    /// Decodes a ServerboundDataStorePacket (324) sent by client DDUI interactions.
    /// </summary>
    public static (string StoreName, string Property, string Value) ReadServerboundDataStore(ref BedrockReader reader)
    {
        string storeName = reader.String();
        string property = reader.String();
        string value = reader.String();
        return (storeName, property, value);
    }
}
