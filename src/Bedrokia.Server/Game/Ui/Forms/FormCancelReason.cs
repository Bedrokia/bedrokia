namespace Bedrokia.Server.Game.Ui.Forms;

/// <summary>
/// Reason why a form was closed or cancelled.
/// </summary>
public enum FormCancelReason : byte
{
    /// <summary>The player closed the form via the X button or Esc.</summary>
    UserClosed = 0,

    /// <summary>The player was busy (another screen opened, inventory opened, etc.).</summary>
    UserBusy = 1,

    /// <summary>The form timed out waiting for a client response.</summary>
    Timeout = 2,

    /// <summary>The player disconnected before answering the form.</summary>
    Disconnected = 3,

    /// <summary>The client returned an invalid, corrupt, or out-of-bounds response.</summary>
    InvalidResponse = 4
}
