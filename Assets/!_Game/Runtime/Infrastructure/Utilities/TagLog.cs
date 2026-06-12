using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BarkingBird.Runtime.Infrastructure.Utilities
{
    // CPIGNORE — Console Pro: skip this wrapper when jumping to source
    public sealed class TagLog
    {
        // Caller file path → colored "[ClassName] " prefix. Relies on the
        // one-class-per-file convention: file name == caller class name.
        private static readonly ConcurrentDictionary<string, string> CallerPrefixes = new();

        private readonly string _tag;
        private readonly string _partTag;

        public TagLog(string tag)
        {
            if (string.IsNullOrEmpty(tag)) {
                _tag = string.Empty;
                _partTag = "[";
            }
            else {
                _tag = '[' + tag + "] ";
                _partTag = '[' + tag + ':';
            }
        }

#if PROD
        [System.Diagnostics.Conditional("DUMMY_UNUSED_DEFINE")]
#endif
        [HideInCallstack]
        public void D(string msg, [CallerFilePath] string caller = "")
        {
            Debug.unityLogger.Log(LogType.Log, _tag, Prefixed(msg, caller));
        }

#if PROD
        [System.Diagnostics.Conditional("DUMMY_UNUSED_DEFINE")]
#endif
        [HideInCallstack]
        public void D(string additionalTag, string msg, [CallerFilePath] string caller = "")
        {
            Debug.unityLogger.Log(LogType.Log, GetFullTag(additionalTag), Prefixed(msg, caller));
        }

#if PROD
        [System.Diagnostics.Conditional("DUMMY_UNUSED_DEFINE")]
#endif
        [HideInCallstack]
        public void D<T>(T colorKey, string msg, [CallerFilePath] string caller = "")
        {
            Debug.unityLogger.Log(LogType.Log, _tag, Prefixed(msg.ColorBasedOnID(colorKey.ToString()), caller));
        }

        [HideInCallstack]
        public void W(string msg, [CallerFilePath] string caller = "")
        {
            Debug.unityLogger.Log(LogType.Warning, _tag, Prefixed(msg, caller));
        }

        [HideInCallstack]
        public void W(string additionalTag, string msg, [CallerFilePath] string caller = "")
        {
            Debug.unityLogger.Log(LogType.Warning, GetFullTag(additionalTag), Prefixed(msg, caller));
        }

        [HideInCallstack]
        public void W<T>(T colorKey, string msg, [CallerFilePath] string caller = "")
        {
            Debug.unityLogger.Log(LogType.Warning, _tag, Prefixed(msg.ColorBasedOnID(colorKey.ToString()), caller));
        }

        [HideInCallstack]
        public void E(string msg, [CallerFilePath] string caller = "")
        {
            Debug.unityLogger.Log(LogType.Error, _tag, Prefixed(msg, caller));
        }

        [HideInCallstack]
        public void E(string additionalTag, string msg, [CallerFilePath] string caller = "")
        {
            Debug.unityLogger.Log(LogType.Error, GetFullTag(additionalTag), Prefixed(msg, caller));
        }

        [HideInCallstack]
        public void E<T>(T colorKey, string msg, [CallerFilePath] string caller = "")
        {
            Debug.unityLogger.Log(LogType.Error, _tag, Prefixed(msg.ColorBasedOnID(colorKey.ToString()), caller));
        }

        [HideInCallstack]
        public void E(Exception e, [CallerFilePath] string caller = "")
        {
            Debug.unityLogger.Log(LogType.Exception, _tag, Prefixed(e.ToString(), caller));
        }

        [HideInCallstack]
        public void E(string additionalTag, Exception e, [CallerFilePath] string caller = "")
        {
            Debug.unityLogger.Log(LogType.Exception, GetFullTag(additionalTag), Prefixed(e.ToString(), caller));
        }

        [HideInCallstack]
        public void ThrowException(string msg)
        {
            throw new Exception(_tag + msg);
        }

        [HideInCallstack]
        public void ThrowException(string additionalTag, string msg)
        {
            throw new Exception(GetFullTag(additionalTag) + msg);
        }

        private string GetFullTag(string additionalTag) => _partTag + additionalTag + "] ";

        private static string Prefixed(string msg, string callerPath)
        {
            if (string.IsNullOrEmpty(callerPath))
                return msg;

            var prefix = CallerPrefixes.GetOrAdd(callerPath, static path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                return $"[{name}]".ColorBasedOnID(name) + " ";
            });
            return prefix + msg;
        }
    }
}
