using System.IO;
using System.Windows;
using System.Drawing;

namespace PictureHandlerWithAsyncAwait;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private CancellationTokenSource _cancelToken = null;

    public MainWindow()
    {
        InitializeComponent();
    }
    private void cmdCancel_Click(object sender, EventArgs e)
    {
        // This will be used to tell all the worker threads to stop!
        _cancelToken.Cancel();
    }

    private async void cmdProcess_Click(object sender, EventArgs e)
    {
        _cancelToken = new CancellationTokenSource();
        var basePath = Directory.GetCurrentDirectory();
        var pictureDirectory = Path.Combine(basePath, "TestPictures");
        var outputDirectory = Path.Combine(basePath, "ModifiedPictures");
        //Clear out any existing files
        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, true);
        }
        Directory.CreateDirectory(outputDirectory);
        string[] files = Directory.GetFiles(pictureDirectory, "*.jpg", SearchOption.AllDirectories);
        try
        {
            foreach (string file in files)
            {
                try
                {
                    await ProcessFileAsync(file, outputDirectory, _cancelToken.Token);
                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            Console.WriteLine(ex);
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
        _cancelToken = null;
        this.Title = "Processing complete";
    }

    private async Task ProcessFileAsync(string currentFile, string outputDirectory, CancellationToken token)
    {
        string filename = Path.GetFileName(currentFile);
        using (Bitmap bitmap = new Bitmap(currentFile))
        {
            try
            {
                await Task.Run(() =>
                    {
                        Dispatcher?.Invoke(() =>
                            {
                                this.Title =
                                    $"Processing {filename}";
                            }
                        );
                        bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        bitmap.Save(Path.Combine(outputDirectory, filename));
                    }
                    , token);
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }

    private static string GetThreadInfo(Thread thread)
    {
        return  $" ThreadId: {thread.ManagedThreadId} " +
                $" IsBackground: {thread.IsBackground} " +
                $" IsAlive: {thread.IsAlive} " +
                $" ThreadState: {thread.ThreadState} ";
    }
   
        private async void cmdProcessWithForEachAsync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ProcessWithForEachAsync();
            }
            catch (Exception ex)
            {
                Title = ex.Message;
            }
        }

        private async Task ProcessWithForEachAsync()
        {
            _cancellationTokenSource = new();

            var basePath = Directory.GetCurrentDirectory();
            var pictureDirectory = Path.Combine(basePath, "TestPictures");
            var outputDirectory = Path.Combine(basePath, "ModifiedPictures");

            // Use ParallelOptions instance to store the CancellationToken.
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.CancellationToken = _cancellationTokenSource.Token;
            parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;

            //Recreate directory 
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
            Directory.CreateDirectory(outputDirectory);

            //Process
            string[] files = Directory.GetFiles(pictureDirectory, "*.jpg", SearchOption.AllDirectories);

            try
            {
                await Parallel.ForEachAsync(files, parallelOptions, async (currentFile, token) =>
                {
                    await Task.Run(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        string filename = Path.GetFileName(currentFile);

                        Thread thread = Thread.CurrentThread;
                        int? taskId = Task.CurrentId;
                        Dispatcher?.Invoke(() => { Title = $"Processing {taskId} {GetThreadInfo(thread)} {filename}"; });

                        using Bitmap bitmap = new Bitmap(currentFile);
                        bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        bitmap.Save(Path.Combine(outputDirectory, filename));
                    });
                });
                Dispatcher?.Invoke(() => Title = "Process complite.");
            }
            catch (OperationCanceledException ex)
            {
                Dispatcher?.Invoke(() => { Title = $"Process canceled! {ex.Message}"; });
            }
        }    
}
