using UdonSharp;
using UnityEngine;
using VRC.Udon;
using VRC.SDKBase;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.Udon.Common.Interfaces;
using UnityEngine.UI;

namespace SerenJson
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class JsonManager : UdonSharpBehaviour
    {
        [Header("Data Source")]
        [Tooltip("The URL of the (Usually) Github Repo file that contains the world state JSON.")]
        [SerializeField] private VRCUrl worldStateUrl;

        [Tooltip("Delay before the first poll, to avoid colliding with other downloads at world load.")]
        [SerializeField] private float startDelay = 2f;

        [Header("AdminUI")]
        [Tooltip("Admin UI refresh button. When pressed, it will send a network event to all clients to refresh the world state.")]
        [SerializeField] private Button refreshButton;

        [Tooltip("How long between each allowed refresh")]
        [SerializeField] private float refreshCooldown = 15f;

        [Header("Listeners")]
        [Tooltip("UdonBehaviour scripts that will receive events when the world state changes. The event to override is OnWorldStateChanged.")]
        [SerializeField] private UdonBehaviour[] ListenerMappings;
        
        [Header("Debug")]
        [Tooltip("Enable debug logging to the console.")]
        [SerializeField] private bool debugLogging = true;

        // Internal variables
        private DataDictionary _rootDictionary;
        private bool _isDataLoaded = false;

        private void Start()
        {
            if (worldStateUrl != null)
            {
                SendCustomEventDelayedSeconds(nameof(RefreshList), startDelay);
            }
        }

        public void TriggerGlobalRefresh()
        {
            DisableRefreshButton();

            SendCustomEventDelayedSeconds(nameof(EnableRefreshButton), refreshCooldown);

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Network_RefreshList));
        }

        public void EnableRefreshButton()
        {
            if (refreshButton != null)
            {
                refreshButton.interactable = true;
            }
        }

        public void DisableRefreshButton()
        {
            if (refreshButton != null)
            {
                refreshButton.interactable = false;
            }
        }

        public void Network_RefreshList()
        {
            _Log("Received global network refresh request. Polling URL...");
            RefreshList();
        }

        // Public Polling Method
        public void RefreshList()
        {
            if (worldStateUrl == null)
                return;

            VRCStringDownloader.LoadUrl(worldStateUrl, this);
            _Log($"Started polling {worldStateUrl}");
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            _Log($"Received {result.Result.Length} chars");
            ParseJson(result.Result);
        }

        private void ParseJson(string json)
        {
            if (!VRCJson.TryDeserializeFromJson(json, out DataToken jsonToken))
            {
                Debug.LogError("[WorldStateManager] JSON Parsing failed! Incorrect format.");
                return;
            }

            if (jsonToken.TokenType != TokenType.DataDictionary)
            {
                Debug.LogError("[WorldStateManager] Top level of JSON is not an object/dictionary.");
                return;
            }

            _rootDictionary = jsonToken.DataDictionary;
            _isDataLoaded = true;
            _Log("JSON successfully parsed into DataDictionary.");

            _UpdateListeners();
        }

        private void _UpdateListeners()
        {
            if (ListenerMappings == null) return;

            foreach (var listener in ListenerMappings)
            {
                if (listener != null)
                {
                    listener.SendCustomEvent("OnWorldStateChanged");
                    _Log($"Sent OnWorldStateChanged to {listener.name}");
                }
            }
        }

        #region Json Key Accessors

        public bool GetBool(string path)
        {
            if (!_isDataLoaded || string.IsNullOrEmpty(path)) return false;

            if (TryGetTokenByPath(_rootDictionary, path, out DataToken token))
            {
                if (token.TokenType == TokenType.Boolean) return token.Boolean;
            }
            return false;
        }

        public string GetString(string path)
        {
            if (!_isDataLoaded || string.IsNullOrEmpty(path)) return string.Empty;

            if (TryGetTokenByPath(_rootDictionary, path, out DataToken token))
            {
                if (token.TokenType == TokenType.String) return token.String;
            }
            return string.Empty;
        }

        public float GetFloat(string path)
        {
            if (!_isDataLoaded || string.IsNullOrEmpty(path)) return 0f;

            if (TryGetTokenByPath(_rootDictionary, path, out DataToken token))
            {
                // Note: VRCJson parses numeric configurations as Doubles by default
                if (token.TokenType == TokenType.Float) return token.Float;
                if (token.TokenType == TokenType.Double) return (float)token.Double;
            }
            return 0f;
        }

        public int GetInt(string path)
        {
            if (!_isDataLoaded || string.IsNullOrEmpty(path)) return 0;

            if (TryGetTokenByPath(_rootDictionary, path, out DataToken token))
            {
                if (token.TokenType == TokenType.Int) return token.Int;
                if (token.TokenType == TokenType.Double) return (int)token.Double;
            }
            return 0;
        }

        public DataList GetArray(string path)
        {
            if (!_isDataLoaded || string.IsNullOrEmpty(path)) return null;

            if (TryGetTokenByPath(_rootDictionary, path, out DataToken token))
            {
                if (token.TokenType == TokenType.DataList) return token.DataList;
            }
            return null;
        }

        #endregion

        private bool TryGetTokenByPath(DataDictionary root, string path, out DataToken finalToken)
        {
            finalToken = default;
            if (root == null || string.IsNullOrEmpty(path)) return false;

            string[] parts = path.Split('.');
            DataDictionary currentDict = root;

            for (int i = 0; i < parts.Length; i++)
            {
                string key = parts[i];

                if (!currentDict.TryGetValue(key, out DataToken token)) return false;

                if (i == parts.Length - 1)
                {
                    finalToken = token;
                    return true;
                }

                if (token.TokenType == TokenType.DataDictionary)
                {
                    currentDict = token.DataDictionary;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            Debug.LogWarning($"[WorldStateDoorController] Load error {result.ErrorCode}: {result.Error}");
        }

        private void _Log(string message)
        {
            if (debugLogging)
                Debug.Log("[WorldStateDoorController] " + message);
        }
    }
}