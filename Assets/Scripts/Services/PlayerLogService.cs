using UnityEngine;
using TMPro;

namespace MMDPlayerForVR.Services
{
    /// <summary>
    /// Service for displaying logs to the player on the UI.
    /// Attach this to a GameObject in the scene and assign a TextMeshProUGUI component.
    /// </summary>
    public class PlayerLogService : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _displayLog;

        /// <summary>
        /// Logs a message to the UI.
        /// </summary>
        public void Log(string message)
        {
            if (_displayLog != null)
            {
                _displayLog.text = $"{message}\n{_displayLog.text}";
            }
            Debug.Log(message);
        }

        /// <summary>
        /// Logs an error message to the UI in red.
        /// </summary>
        public void LogError(string message)
        {
            if (_displayLog != null)
            {
                _displayLog.text = $"<color=red>Error: {message}</color>\n{_displayLog.text}";
            }
            Debug.LogError(message);
        }
    }
}
