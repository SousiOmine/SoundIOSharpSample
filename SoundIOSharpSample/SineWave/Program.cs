﻿using SoundIOSharp;

namespace SineWave
{
	class Program
	{
		static double seconds_offset = 0.0;
		static Action<IntPtr, double>? write_sample;

		static void Main()
		{
			var soundIo = new SoundIO();
			soundIo.Connect();

			Console.WriteLine("Backend: " + soundIo.CurrentBackend);

			soundIo.FlushEvents();

			var device = soundIo.GetOutputDevice(soundIo.DefaultOutputDeviceIndex);

			Console.WriteLine("Output device: " + device.Name);

			var outstream = device.CreateOutStream();

			outstream.WriteCallback = (min, max) => write_callback(outstream, min, max);

			outstream.SampleRate = 44100;
			if (device.SupportsFormat(SoundIODevice.Float32FE))
			{
				outstream.Format = SoundIODevice.Float32FE;
				write_sample = write_sample_float32ne;
			}
			else if (device.SupportsFormat(SoundIODevice.Float64NE))
			{
				outstream.Format = SoundIODevice.Float64NE;
				write_sample = write_sample_float64ne;
			}
			else if (device.SupportsFormat(SoundIODevice.S32NE))
			{
				outstream.Format = SoundIODevice.S32NE;
				write_sample = write_sample_s32ne;
			}
			else if (device.SupportsFormat(SoundIODevice.S16NE))
			{
				outstream.Format = SoundIODevice.S16NE;
				write_sample = write_sample_s16ne;
			}
			else
			{
				Console.Error.WriteLine("No suitable format available.");
				return;
			}
			
			Console.WriteLine("Format: " + outstream.Format);

			outstream.Open();
			outstream.Start();

			Console.ReadLine();

			outstream.Dispose();
			device.RemoveReference();
			soundIo.Dispose();

			return;


		}

		static void write_callback(SoundIOOutStream outstream, int frame_count_min, int frame_count_max)
		{
			double float_sample_rate = outstream.SampleRate;
			double seconds_per_frame = 1.0 / float_sample_rate;

			int frames_left = frame_count_max;
			int frame_count = 0;

			for (; ; )
			{
				frame_count = frames_left;
				var results = outstream.BeginWrite(ref frame_count);

				if (frame_count == 0)
					break;

				SoundIOChannelLayout layout = outstream.Layout;

				double pitch = 440.0;
				double radians_per_second = pitch * 2.0 * Math.PI;
				for (int frame = 0; frame < frame_count; frame += 1)
				{
					double sample = Math.Sin((seconds_offset + frame * seconds_per_frame) * radians_per_second);
					for (int channel = 0; channel < layout.ChannelCount; channel += 1)
					{

						var area = results.GetArea(channel);
						write_sample(area.Pointer, sample);
						area.Pointer += area.Step;
					}
				}
				seconds_offset = Math.IEEERemainder(seconds_offset + seconds_per_frame * frame_count, 1.0);

				outstream.EndWrite();

				frames_left -= frame_count;
				if (frames_left <= 0)
					break;
			}
		}

		static unsafe void write_sample_s16ne(IntPtr ptr, double sample)
		{
			short* buf = (short*)ptr;
			double range = (double)short.MaxValue - (double)short.MinValue;
			double val = sample * range / 2.0;
			*buf = (short)val;
		}

		static unsafe void write_sample_s32ne(IntPtr ptr, double sample)
		{
			int* buf = (int*)ptr;
			double range = (double)int.MaxValue - (double)int.MinValue;
			double val = sample * range / 2.0;
			*buf = (int)val;
		}

		static unsafe void write_sample_float32ne(IntPtr ptr, double sample)
		{
			float* buf = (float*)ptr;
			*buf = (float)sample;
		}

		static unsafe void write_sample_float64ne(IntPtr ptr, double sample)
		{
			double* buf = (double*)ptr;
			*buf = sample;
		}
	}
}