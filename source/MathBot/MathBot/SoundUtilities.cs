using SharpDX.IO;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBot
{
    public static class SoundUtilities
    {
        public static float Volume { get; set; } = 1;

        private static Dictionary<string, SourceVoice> LoadedSounds = new Dictionary<string, SourceVoice>();
        private static Dictionary<string, AudioBufferAndMetaData> AudioBuffers = new Dictionary<string, AudioBufferAndMetaData>();
        private static MasteringVoice m_MasteringVoice;
        public static MasteringVoice MasteringVoice
        {
            get
            {
                if (m_MasteringVoice == null)
                {
                    m_MasteringVoice = new MasteringVoice(XAudio);
                    m_MasteringVoice.SetVolume(1, 0);
                }
                return m_MasteringVoice;
            }
        }
        private static XAudio2 m_XAudio;
        public static XAudio2 XAudio
        {
            get
            {
                if (m_XAudio == null)
                {
                    m_XAudio = new XAudio2();
                    var voice = MasteringVoice; //touch voice to create it
                    m_XAudio.StartEngine();
                }
                return m_XAudio;
            }
        }
        public static void PlaySound(string soundfile)
        {
            SourceVoice sourceVoice;
            if (!LoadedSounds.ContainsKey(soundfile))
            {

                var buffer = GetBuffer(soundfile);
                sourceVoice = new SourceVoice(XAudio, buffer.WaveFormat, true);
                sourceVoice.SetVolume(Volume, SharpDX.XAudio2.XAudio2.CommitNow);
                sourceVoice.SubmitSourceBuffer(buffer, buffer.DecodedPacketsInfo);
                sourceVoice.Start();
            }
            else
            {
                sourceVoice = LoadedSounds[soundfile];
                if (sourceVoice != null)
                    sourceVoice.Stop();
            }
        }

        public static Task PlaySound(Stream stream)
        {
            var soundstream = new SoundStream(stream);
            var buffer = new AudioBufferAndMetaData()
            {
                Stream = soundstream.ToDataStream(),
                AudioBytes = (int)soundstream.Length,
                Flags = BufferFlags.EndOfStream,
                WaveFormat = soundstream.Format,
                DecodedPacketsInfo = soundstream.DecodedPacketsInfo
            };

            var sourceVoice = new SourceVoice(XAudio, buffer.WaveFormat, true);
            sourceVoice.SetVolume(Volume, SharpDX.XAudio2.XAudio2.CommitNow);
            sourceVoice.SubmitSourceBuffer(buffer, buffer.DecodedPacketsInfo);


            //var effect = new SharpDX.XAPO.Fx.Echo(XAudio);
            //EffectDescriptor effectDescriptor = new EffectDescriptor(effect);
            //sourceVoice.SetEffectChain(effectDescriptor);
            //sourceVoice.EnableEffect(0);

            sourceVoice.Start();

            TaskCompletionSource<object> mediaDone = new TaskCompletionSource<object>();

            sourceVoice.StreamEnd += () => {
                mediaDone.SetResult(null);
            };

            return mediaDone.Task;
        }



        private static AudioBufferAndMetaData GetBuffer(string soundfile)
        {
            if (!AudioBuffers.ContainsKey(soundfile))
            {
                var nativefilestream = new NativeFileStream(
                        soundfile,
                        NativeFileMode.Open,
                        NativeFileAccess.Read,
                        NativeFileShare.Read);

                var soundstream = new SoundStream(nativefilestream);

                var buffer = new AudioBufferAndMetaData()
                {
                    Stream = soundstream.ToDataStream(),
                    AudioBytes = (int)soundstream.Length,
                    Flags = BufferFlags.EndOfStream,
                    WaveFormat = soundstream.Format,
                    DecodedPacketsInfo = soundstream.DecodedPacketsInfo
                };
                AudioBuffers[soundfile] = buffer;
            }
            return AudioBuffers[soundfile];

        }
        private sealed class AudioBufferAndMetaData : AudioBuffer
        {
            public WaveFormat WaveFormat { get; set; }
            public uint[] DecodedPacketsInfo { get; set; }
        }
    }
}