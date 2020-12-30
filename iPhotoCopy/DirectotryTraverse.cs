using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DirectoryTraverse
{

    public static class DirectoryUtill 
	{
        public static string targetDir = @"/Volumes/UserData/iPhotoBackup";
		public static void TestDirectoryUtill()
		{
            

            string sourceDir = @"/Users/gopal/Pictures/Photos Library.photoslibrary/originals";

			try
			{
				TraverseTreeParallelForEach(sourceDir, (f) =>
				{
					// Exceptions are no-ops.
					try
					{
						// Do nothing with the data except read it.
						//byte[] data = File.ReadAllBytes(f);
						FileInfo fi = new FileInfo(f);

                        
                        var directory = fi.Directory;
                        var folderInfo = fi.CreationTime.Year.ToString();
                        var filePrefix = fi.CreationTime.Month.ToString() + fi.CreationTime.Day.ToString("D2");
                        StringBuilder builder = new StringBuilder(filePrefix);
                        builder.Append("_").Append(fi.Name);
                        var newFileName = builder.ToString();

                        var targetPath = Path.Combine(targetDir, folderInfo);
                        var targetFilePath = Path.Combine(targetDir, folderInfo, newFileName);

						// To copy a folder's contents to a new location:
						// Create a new target folder, if necessary.
						if (!System.IO.Directory.Exists(targetPath))
						{
							System.IO.Directory.CreateDirectory(targetPath);
						}

                        if(!System.IO.File.Exists(targetFilePath))
                        {
                            Console.WriteLine("New file  " + targetFilePath);
                        }

                        System.IO.File.Copy(f, targetFilePath, false);

                        //FileInfo file = new FileInfo("/Volumes/Backup/iPhotoBackup/log.csv");
                        //StreamWriter sw = file.AppendText();
                        //StringBuilder sb = new StringBuilder(f);
                        //sb.Append(",").Append(fi.Length).Append(",").Append(targetPath).Append(",").Append(targetFilePath);
                        //sw.WriteLine(sb.ToString());
                        //sw.Flush();
                        //sw.Close();
						
					}
					catch (FileNotFoundException) { }
					catch (IOException) { }
					catch (UnauthorizedAccessException) { }
					catch (SecurityException) { }
					// Display the filename.
					//Console.WriteLine(f);
				});
			}
			catch (ArgumentException ex)
			{
                Console.WriteLine(@"The directory does not exist." + ex.Message);
			}

		}

		static void TraverseTreeParallelForEach(string root, Action<string> action)
		{
			//Count of files traversed and timer for diagnostic output
			int fileCount = 0;
			var sw = Stopwatch.StartNew();

			// Determine whether to parallelize file processing on each folder based on processor count.
			int procCount = System.Environment.ProcessorCount;

			// Data structure to hold names of subfolders to be examined for files.
			Stack<string> dirs = new Stack<string>();

			if (!Directory.Exists(root))
			{
				throw new ArgumentException();
			}
			dirs.Push(root);

			while (dirs.Count > 0)
			{
				string currentDir = dirs.Pop();
				string[] subDirs = { };
				string[] files = { };

				try
				{
					subDirs = Directory.GetDirectories(currentDir);
				}
				// Thrown if we do not have discovery permission on the directory.
				catch (UnauthorizedAccessException e)
				{
					Console.WriteLine(e.Message);
					continue;
				}
				// Thrown if another process has deleted the directory after we retrieved its name.
				catch (DirectoryNotFoundException e)
				{
					Console.WriteLine(e.Message);
					continue;
				}

				try
				{
					files = Directory.GetFiles(currentDir);
				}
				catch (UnauthorizedAccessException e)
				{
					Console.WriteLine(e.Message);
					continue;
				}
				catch (DirectoryNotFoundException e)
				{
					Console.WriteLine(e.Message);
					continue;
				}
				catch (IOException e)
				{
					Console.WriteLine(e.Message);
					continue;
				}

				// Execute in parallel if there are enough files in the directory.
				// Otherwise, execute sequentially.Files are opened and processed
				// synchronously but this could be modified to perform async I/O.
				try
				{
					if (files.Length < procCount)
					{
						foreach (var file in files)
						{
							action(file);
							fileCount++;
						}
					}
					else
					{
						Parallel.ForEach(files, () => 0, (file, loopState, localCount) =>
													 {
														 action(file);
														 return (int)++localCount;
													 },
										 (c) =>
										 {
											 Interlocked.Add(ref fileCount, c);
										 });
                        //foreach(var file in files)
                        //{
                        //    action(file);
                        //}
					}
				}
				catch (AggregateException ae)
				{
					ae.Handle((ex) =>
					{
						if (ex is UnauthorizedAccessException)
						{
							// Here we just output a message and go on.
							Console.WriteLine(ex.Message);
							return true;
						}
						// Handle other exceptions here if necessary...

						return false;
					});
				}

				// Push the subdirectories onto the stack for traversal.
				// This could also be done before handing the files.
				foreach (string str in subDirs)
					dirs.Push(str);
			}

            // For diagnostic purposes.
            Console.WriteLine("Process" + "ed {0} files in {1} milleseconds", fileCount, sw.ElapsedMilliseconds);
		}


	}
}