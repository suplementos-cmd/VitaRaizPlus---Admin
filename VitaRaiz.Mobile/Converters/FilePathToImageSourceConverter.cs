using System.Globalization;

namespace VitaRaiz.Mobile.Converters;

/// <summary>
/// Converts a file path string to an ImageSource for MAUI Image control
/// Returns null if path is null/empty or file doesn't exist
/// </summary>
public class FilePathToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath || string.IsNullOrEmpty(filePath))
            return null;

        try
        {
            // Check if file exists before attempting to load
            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"[FilePathToImageSourceConverter] File not found: {filePath}");
                return null;
            }

            // Create ImageSource from file path
            return ImageSource.FromFile(filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FilePathToImageSourceConverter] Error loading image: {ex.Message}");
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
