using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace PulseEngine
{

    public class MainThread : MonoBehaviour
    {
        #region Constants #############################################################

        const int WaiTimeMs = 50;

        #endregion

        #region Variables #############################################################

        private static MainThread _instance;

        private List<Action> _actionPool = new List<Action>();

        private List<CancellationTokenSource> _ctsPool = new List<CancellationTokenSource>();

        #endregion

        #region Statics   #############################################################

        /// <summary>
        /// Automatically creates the object
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreation()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("~MainThreadHost");
            _instance = go.AddComponent<MainThread>();
            GameObject.DontDestroyOnLoad(go);
        }

        /// <summary>
        /// Execute action on the main thread.
        /// </summary>
        /// <param name="action">action to execute</param>
        public static async Task Execute(Action action, CancellationToken ct = default)
        {
            if (!_instance)
                return;
            var cts = new CancellationTokenSource();
            Action act = () =>
            {
                action?.Invoke();
                cts.Cancel();
            };
            _instance._ctsPool.Add(cts);
            _instance._actionPool.Add(act);
            await WaitUntil(() => cts.IsCancellationRequested, ct);
        }

        /// <summary>
        /// Execute action on the main thread.
        /// </summary>
        /// <param name="func">action to execute</param>
        public static async Task<T> Execute<T>(Func<T> func, CancellationToken ct = default)
        {
            if (!_instance)
                return default;
            if (func == null)
                return default;
            var cts = new CancellationTokenSource();
            T result = default;
            Action act = () =>
            {
                result = func.Invoke();
                cts.Cancel();
            };
            _instance._ctsPool.Add(cts);
            _instance._actionPool.Add(act);
            await WaitUntil(() => cts.IsCancellationRequested, ct);
            return result;
        }

        /// <summary>
        /// Execute action on the main thread after an unscaled delay.
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="delay">The dealy in ms</param>
        public static async Task DelayedExecution(Action action, int delay, CancellationToken ct = default)
            => await Execute(async () => { await Task.Delay(delay, ct); if (!ct.IsCancellationRequested) action?.Invoke(); }, ct);

        /// <summary>
        /// Execute action if condition is meet.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="condition"></param>
        public static void ConditionnaleExecution(Action action, Func<bool> condition) { }

        /// <summary>
        /// Wait for a condition to be meet.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static async Task WaitUntil(Func<bool> predicate, CancellationToken ct = default)
        {
            if (!_instance)
                return;
            if (predicate == null)
                return;
            while (!ct.IsCancellationRequested)
            {
                if (predicate.Invoke())
                {
                    break;
                }
                await Task.Yield();
            }
        }

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        #endregion

        #region Public Functions ######################################################

        #endregion

        #region Private Functions #####################################################

        #endregion

        #region Jobs      #############################################################

        #endregion

        #region MonoBehaviours ########################################################

        private void OnEnable()
        {
            _instance = this;
        }

        private void Update()
        {
            if (_actionPool.Count > 0)
            {
                for (int i = 0; i < _actionPool.Count; i++)
                {
                    _actionPool[i]?.Invoke();
                }

                for (int i = _ctsPool.Count - 1; i >= 0; i--)
                {
                    if (_ctsPool[i].IsCancellationRequested)
                    {
                        _ctsPool[i].Dispose();
                        _ctsPool.RemoveAt(i);
                        _actionPool.RemoveAt(i);
                    }
                }
            }
        }

        #endregion
    }

}