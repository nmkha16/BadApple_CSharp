using System;
using System.Diagnostics;
using System.Media;
using System.Text;
using Emgu.CV;


namespace BadApple{
    class Program{
        static int maxThreads = 6;
        static int frameRate = 30;
        static int width = 80;
        static int height = 50;
        static int nframeConverted = 0;

        public static void Main(string[] args){
            //SET DEFAULT SIZE
			Console.SetWindowSize(width,height);
            int nframes;
            float frameRateInterval = 1000 / (float)frameRate;


            string home = "D:\\Study\\something\\BadAppleASCII";
            // EXTRACTING FRAME
            ExtractFrame(Path.Combine(home , "video\\badapple.mp4"), Path.Combine(home,"frames"),out nframes);
            nframes = 1000;
            Console.Clear();

            // CONVERSION TO ASCII
            string[] asciiFrames = new string[nframes];

            // MUST SORT FRAME FILES
            var frameFiles = Directory.GetFiles(Path.Combine(home,"frames"));
            var sorted =  frameFiles.Select(f=> new FileInfo(f)).OrderBy(f=> f.CreationTime);
            {
                int i =0;
                foreach(var s in sorted){
                    frameFiles[i++] = s.ToString();
                }
            }

            var step = nframes / maxThreads;
            for (int t = 0; t < maxThreads; t++){
                var from =  step * t;
                var to = (t+1) * step;
                var tid = t;
                if (t == maxThreads-1){
                    to += nframes - to;
                }
                
                Thread thread = new(() => ConvertToASCII(tid,frameFiles, asciiFrames,from,to));
                thread.Start();
            }

            // WAIT until all frames ascii converting complete
            while (nframeConverted != nframes){
                // maybe do something on progressbar
            }

            Console.Clear();

            // Play AUDIO
            string audioPath = Path.Combine(home,"audio.wav");
            var audio = new SoundPlayer(audioPath);
            audio.Play();
            
            // PLAYING FRAMES
            Stopwatch playbackWatch = new Stopwatch();
            playbackWatch.Start();
            int currentPlayingFrame = 0;
            while (currentPlayingFrame < nframes){
                if (playbackWatch.ElapsedMilliseconds % (int)frameRateInterval == 0){
                    int frameIndex = (int)Math.Floor(playbackWatch.ElapsedMilliseconds / frameRateInterval);
                    
                    if (frameIndex >= nframes)
                    {
                        break;
                    }

                    if (currentPlayingFrame != frameIndex){
                        Console.SetCursorPosition(0,0);
                        FastConsole.WriteLine(asciiFrames[frameIndex]);
                        FastConsole.Flush();
                        currentPlayingFrame = frameIndex;   

                        Console.Title = "Frame " + currentPlayingFrame.ToString();
                    }
                }
            }

            playbackWatch.Stop();
            audio.Stop();
            audio.Dispose();
            Console.ReadLine();
        }

        private static void ConvertToASCII(int tid, string[] frameFiles, string[] asciiFrames, int from, int to){
            for (int i = from; i < to; ++i)
            {
                string ascii = Utils.Converter.ASCIIConverter(frameFiles[i],width,height);
                asciiFrames[i] = ascii;
                nframeConverted++;
            }
            Console.WriteLine($"Thread {tid} done converting to ascii!");
        }

        private static void ExtractFrame(string path, string output, out int totalFrame){
            int i = 0;
            var watch = new System.Diagnostics.Stopwatch();

            watch.Start();
            using (var video = new VideoCapture(path)){
                totalFrame = (int) video.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                using var img = new Mat();
                while (video.Grab())
                {
                    var filename = Path.Combine(output, $"{i++}.bmp");
                    // before retrieving and writing bmp we should check if file already existed?
                    if (File.Exists(filename)){
                        // Console.WriteLine(filename + " existed.");
                        continue;
                    }
                    video.Retrieve(img);
                    CvInvoke.Imwrite(filename, img);
                }
                Console.WriteLine($"Done extracting frame in {watch.ElapsedMilliseconds/1000} s!");
            }
            watch.Stop();
        }



        // private static void ExtractFrameMultiThread(string path, string output, out int totalFrame){
        //     //var watch = new System.Diagnostics.Stopwatch();

        //     //watch.Start();
        //     using (var video = new VideoCapture(path)){
        //         totalFrame = (int)video.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
        //         Console.WriteLine("Video has " + totalFrame.ToString() + " frames!");

        //         int step = totalFrame /maxThreads;
        //         for (int t = 0; t < maxThreads; t++){
        //             var from =  step * t;
        //             var to = (t+1) * step;
        //             var tid = t;
        //             if (t == maxThreads-1){
        //                 to += totalFrame - to;
        //             }
                    
        //             Thread thread = new(() => InternalExtractFrame(tid,path,output,from,to));
        //             thread.Start();
        //         }
        //     }
        // }

        // private static void InternalExtractFrame(int threadIdx, string videoPath, string output, int from, int to){
        //     var watch = new System.Diagnostics.Stopwatch();
        //     int i = from;
        //     watch.Start();
        //     using (var v = new VideoCapture(videoPath)){
        //         using var img = new Mat();
        //         v.Set(Emgu.CV.CvEnum.CapProp.PosFrames,i);
        //         while(v.Grab() && i < to){
        //             v.Retrieve(img);
        //             var filename = Path.Combine(output, $"{i++}.bmp");
        //             CvInvoke.Imwrite(filename, img);
        //         }
        //     }
        //     watch.Stop();
        //     Console.WriteLine($"Thread {threadIdx} done extracting frame in {watch.ElapsedMilliseconds/1000} s!");
        // }
        
    }

    //FROM https://stackoverflow.com/questions/5272177/console-writeline-slow
	public static class FastConsole
	{
		static readonly BufferedStream str;

		static FastConsole()
		{
			Console.OutputEncoding = Encoding.Unicode;  // crucial

			// avoid special "ShadowBuffer" for hard-coded size 0x14000 in 'BufferedStream' 
			str = new BufferedStream(Console.OpenStandardOutput(), 0x15000);
		}

		public static void WriteLine(String s) => Write(s + "\r\n");

		public static void Write(String s)
		{
			// avoid endless 'GetByteCount' dithering in 'Encoding.Unicode.GetBytes(s)'
			var rgb = new byte[s.Length << 1];
			Encoding.Unicode.GetBytes(s, 0, s.Length, rgb, 0);

			lock (str)   // (optional, can omit if appropriate)
				str.Write(rgb, 0, rgb.Length);
		}

		public static void Flush() { lock (str) str.Flush(); }
	};
}
