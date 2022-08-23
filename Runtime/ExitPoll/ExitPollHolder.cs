﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D;

//the idea is that you could set up this component with your settings, then just call 'Activate'
//easier to visualize all the valid options, easier to override stuff
namespace Cognitive3D
{
    public class ExitPollHolder : MonoBehaviour
    {
        public ExitPollParameters Parameters = new ExitPollParameters();
        public bool ActivateOnEnable;

        private void OnEnable()
        {
            if (ActivateOnEnable)
            {
                //will wait for cognitive vr manager to have call initialize before activating
                if (Cognitive3D.Core.IsInitialized)
                {
                    Activate();
                }
                else
                {
                    Cognitive3D.Core.InitEvent += Core_DelayInitEvent;
                }
            }
        }

        private void Core_DelayInitEvent(Error initError)
        {
            Cognitive3D.Core.InitEvent -= Core_DelayInitEvent;
            if (initError == Error.None)
            {
                OnEnable();
            }
        }
        
        /// <summary>
        /// Display an ExitPoll using the QuestionSetHook and parameters configured on the component
        /// </summary>
        public void Activate()
        {
            var poll = ExitPoll.NewExitPoll(Parameters.Hook, Parameters);

            if (poll.ExitpollSpawnType == ExitPoll.SpawnType.World)
            {
                poll.UseOverridePosition = true;
                poll.OverridePosition = transform.position;
                poll.UseOverrideRotation = true;
                poll.OverrideRotation = transform.rotation;
                poll.RotateToStayOnScreen = false;
                poll.LockYPosition = false;
            }

            poll.Begin();
        }
    }
}