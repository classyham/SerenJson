using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SerenJson
{   
    public class JsonListenerBase : UdonSharpBehaviour
    {

        [Header("Data")]

        [Tooltip("The Json Manager script.")]
        [SerializeField] protected JsonManager jsonManager;

        [Tooltip("The JSON key that will be read from the world state. The key uses dot notation to access nested objects, e.g. 'WorldPerms.Admin'.")]
        [SerializeField] protected string jsonKey;

        [Tooltip("Additional JSON keys to read from the world state. The keys use dot notation to access nested objects, e.g. 'WorldPerms.Admin'.")]
        [SerializeField] protected string[] additionalJsonKeys;

        [Header("Debug")]
        [Tooltip("Enable debug logging to the console.")]
        [SerializeField] protected bool debugLogging = true;

        public virtual void OnWorldStateChanged()
        {
             //To be overridden by child classes
        }
    }
}