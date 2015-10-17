﻿/**
* Copyright 2015 IBM Corp. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
* @author Richard Lyle (rolyle@us.ibm.com)
*/

using IBM.Watson.Utilities;
using System.Collections;
using UnityEngine;

#pragma warning disable 414

namespace IBM.Watson.Widgets
{
    /// <summary>
    /// This widget records audio from the microphone device.
    /// </summary>
    class MicrophoneWidget : Widget
    {
        #region Public Properties
        /// <summary>
        /// Returns a list of available microphone devices.
        /// </summary>
        public string[] Microphones { get { return Microphone.devices; } }
        /// <summary>
        /// 
        /// </summary>
        public bool Active
        {
            get { return m_RecordingRoutine != 0; }
            set
            {
                if (value)
                    StartRecording();
                else
                    StopRecording();
            }
        }
        public void OnToggleActive()
        {
            Active = !Active;
        }
        #endregion

        #region Widget interface
        protected override string GetName()
        {
            return "Microphone";
        }
        #endregion

        #region Private Data
        [SerializeField]
        private Input m_ActivateInput = new Input("Activate", typeof(BooleanData), "OnActivate");
        [SerializeField]
        private Output m_AudioOutput = new Output(typeof(AudioData));
        [SerializeField]
        private Output m_LevelOutput = new Output(typeof(FloatData));
        [SerializeField]
        private Output m_ActivateOutput = new Output(typeof(BooleanData));
        [SerializeField, Tooltip("Size of recording buffer in seconds.")]
        private int m_RecordingBufferSize = 2;
        [SerializeField]
        private int m_RecordingHZ = 22050;                  // default recording HZ
        [SerializeField, Tooltip("ID of the microphone to use.")]
        private string m_MicrophoneID = null;               // what microphone to use for recording.
        [SerializeField, Tooltip("How often to sample for level output.")]
        private float m_LevelOutputInterval = 0.05f;

        private int m_RecordingRoutine = 0;                      // ID of our co-routine when recording, 0 if not recording currently.
        private AudioClip m_Recording = null;
        #endregion

        #region Private Functions
        private void OnActivate(Data data)
        {
            Active = ((BooleanData)data).Boolean;
            m_ActivateOutput.SendData( data );
        }

        private void StartRecording()
        {
            if (m_RecordingRoutine == 0)
                m_RecordingRoutine = Runnable.Run(RecordingHandler());
        }

        private void StopRecording()
        {
            if (m_RecordingRoutine != 0)
            {
                Microphone.End(m_MicrophoneID);
                Runnable.Stop(m_RecordingRoutine);
                m_RecordingRoutine = 0;
                m_Recording = null;
            }
        }

        private IEnumerator RecordingHandler()
        {
#if UNITY_WEBPLAYER
            yield return Application.RequestUserAuthorization( UserAuthorization.Microphone );
#endif
            m_Recording = Microphone.Start(m_MicrophoneID, true, m_RecordingBufferSize, m_RecordingHZ);
            yield return null;      // let m_RecordingRoutine get set..

            bool bFirstBlock = true;
            int midPoint = m_Recording.samples / 2;

            bool bOutputLevelData = m_LevelOutput.IsConnected;
            int lastReadPos = 0;
            float [] samples = null;

            while (m_RecordingRoutine != 0)
            {
                int writePos = Microphone.GetPosition(m_MicrophoneID);
                if ((bFirstBlock && writePos >= midPoint)
                    || (!bFirstBlock && writePos < midPoint))
                {
                    // front block is recorded, make a RecordClip and pass it onto our callback.
                    samples = new float[midPoint];
                    m_Recording.GetData(samples, bFirstBlock ? 0 : midPoint);

                    AudioData record = new AudioData();
                    record.MaxLevel = Mathf.Max(samples);
                    record.Clip = AudioClip.Create("Recording", midPoint, m_Recording.channels, m_RecordingHZ, false);
                    record.Clip.SetData(samples, 0);

                    if (!m_AudioOutput.SendData(record))
                        StopRecording();        // automatically stop recording if the callback goes away.

                    bFirstBlock = !bFirstBlock;
                }
                else
                {
                    // calculate the number of samples remaining until we ready for a block of audio, 
                    // and wait that amount of time it will take to record.
                    int remaining = bFirstBlock ? (midPoint - writePos) : (m_Recording.samples - writePos);
                    float timeRemaining = (float)remaining / (float)m_RecordingHZ;
                    if ( bOutputLevelData && timeRemaining > m_LevelOutputInterval )
                        timeRemaining = m_LevelOutputInterval;                                        
                    yield return new WaitForSeconds(timeRemaining);
                }

                if ( bOutputLevelData )
                {
                    float fLevel = 0.0f;
                    float fDivisor = 0.0f;

                    if ( writePos < lastReadPos )
                    {
                        // write has wrapped, grab the last bit from the buffer..
                        samples = new float[ m_Recording.samples - lastReadPos ];
                        m_Recording.GetData( samples, lastReadPos );
                        for(int i=0;i<samples.Length;++i)
                            fLevel += Mathf.Abs( samples[i] );
                        fDivisor += samples.Length;       // average them..

                        lastReadPos = 0;
                    }

                    if ( lastReadPos < writePos )
                    {
                        samples = new float[writePos - lastReadPos];
                        m_Recording.GetData( samples, lastReadPos );
                        for(int i=0;i<samples.Length;++i)
                            fLevel += Mathf.Abs( samples[i] );
                        fDivisor += samples.Length;       // average them..

                        lastReadPos = writePos;
                    }

                    fLevel /= fDivisor;
                    m_LevelOutput.SendData( new FloatData( fLevel ) );
                }
            }

            yield break;
        }
        #endregion
    }
}