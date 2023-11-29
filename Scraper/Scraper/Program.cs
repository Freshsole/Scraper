using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Enter the Shopify product URL:");
        string shopifyUrl = Console.ReadLine();

        try
        {
            await DownloadImagesFromShopifyProduct(shopifyUrl);
            Console.WriteLine("Images downloaded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task DownloadImagesFromShopifyProduct(string shopifyUrl)
    {
        using (HttpClient client = new HttpClient())
        {
            // Set a user agent to mimic a web browser
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            // Extract product name from the URL
            string productName = ExtractProductNameFromUrl(shopifyUrl);

            // Sanitize product name for folder
            string folderName = SanitizeFolderName(productName);

            // Download HTML content of the Shopify product URL
            string htmlContent = await client.GetStringAsync(shopifyUrl);

            // Parse HTML using HtmlAgilityPack
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(htmlContent);

            // Extract image URLs from the HTML
            var imageNodes = htmlDocument.DocumentNode.SelectNodes("//img[contains(@src, 'cdn.shopify.com') and contains(@src, '/files/')]");

            if (imageNodes != null)
            {
                foreach (var imageNode in imageNodes)
                {
                    string imageUrl = imageNode.GetAttributeValue("src", null);

                    // Filter images based on the product name
                    if (ContainsAllWords(imageUrl, productName.Split(' ')))
                    {
                        // Ensure the URL is absolute
                        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
                        {
                            imageUrl = new Uri(new Uri(shopifyUrl), imageUrl).AbsoluteUri;
                        }

                        Console.WriteLine($"Processing image URL: {imageUrl}");
                        await DownloadImageAsync(imageUrl, folderName);
                    }
                    else
                    {
                        Console.WriteLine($"Excluded image URL: {imageUrl}");
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("No images found in the HTML.");
            }
        }
    }

    static string ExtractProductNameFromUrl(string url)
    {
        // Extract the product name from the URL
        string[] urlSegments = url.Split('/');
        string productName = urlSegments[urlSegments.Length - 1];

        // Replace hyphens with spaces
        productName = productName.Replace('-', ' ');

        // Remove query parameters
        int queryIndex = productName.IndexOf('?');
        if (queryIndex != -1)
        {
            productName = productName.Substring(0, queryIndex);
        }

        return productName.Trim();
    }

    static bool ContainsAllWords(string text, string[] words)
    {
        // Check if the text contains all the specified words
        return words.All(word => text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    static bool ShouldExcludeImage(string imageUrl)
    {
        // URLs to always exclude
        string[] excludeUrls = {
        "https://i0.wp.com/sectionstore.cz/wp-content/uploads/2021/01/doublebox.jpg?fit=1000%2C195&amp;ssl=1",
        "https://i0.wp.com/sectionstore.cz/wp-content/uploads/2022/06/doprava.jpg?fit=1000%2C195&amp;ssl=1",
        "https://i0.wp.com/sectionstore.cz/wp-content/uploads/2022/11/limited.jpg?fit=1000%2C195&amp;ssl=1",
        "https://i0.wp.com/sectionstore.cz/wp-content/uploads/2021/01/legit.jpg?fit=1000%2C195&amp;ssl=1",
        "https://sectionstore.cz/wp-content/uploads/2022/11/linktree-facebook.png",
        "https://sectionstore.cz/wp-content/uploads/2022/11/linktree-ig.png",
        "https://sectionstore.cz/wp-content/uploads/2022/11/linktree-tiktok.png",
        "https://sectionstore.cz/wp-content/uploads/2022/11/linktree-google.png",
        "https://i0.wp.com/sectionstore.cz/wp-content/uploads/2022/11/jordan-1-high-menu.png?fit=500%2C300&amp;ssl=1",
        "https://i0.wp.com/sectionstore.cz/wp-content/uploads/2022/11/menu-trika.png?fit=500%2C300&amp;ssl=1",
        "https://i0.wp.com/sectionstore.cz/wp-content/uploads/2020/02/section_logo_web.png?fit=1288%2C575&amp;ssl=1"
    };

        return excludeUrls.Any(url => imageUrl.IndexOf(url, StringComparison.OrdinalIgnoreCase) >= 0);
    }


    static async Task DownloadImageAsync(string imageUrl, string folderName)
    {
        using (HttpClient client = new HttpClient())
        {
            // Download image bytes
            byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);

            // Extract filename from the URL
            string fileName = GetFileNameFromUrl(imageUrl);

            // Získání aktuálního adresáře projektu
            string projectDirectory = Directory.GetCurrentDirectory();

            // Nebo alternativně:
            // string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Přidání složky s obrázky k adresáři projektu
            string directoryPath = Path.Combine(projectDirectory, "Scraped Images", folderName);

            // Vytvoření složky, pokud neexistuje
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }


            // Create the folder if it doesn't exist
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Save the image to the folder
            string filePath = Path.Combine(directoryPath, fileName);
            File.WriteAllBytes(filePath, imageBytes);
        }
    }

    static string GetFileNameFromUrl(string url)
    {
        Uri uri = new Uri(url);
        string fileName = Path.GetFileName(uri.LocalPath);
        return fileName;
    }

    static string SanitizeFolderName(string folderName)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            folderName = folderName.Replace(invalidChar, '_');
        }

        // Remove numbers and replace hyphens with spaces
        folderName = new string(folderName.Where(c => !char.IsDigit(c)).ToArray()).Replace("-", " ");

        // Capitalize each word
        folderName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(folderName);

        return folderName;
    }

    static void RenameFolder(string folderPath, string newFolderName)
    {
        string newFolderPath = Path.Combine(Path.GetDirectoryName(folderPath), newFolderName);
        Directory.Move(folderPath, newFolderPath);
        Console.WriteLine($"Folder renamed to: {newFolderName}");
    }

    static string GetFirstImageNameInFolder(string folderPath)
    {
        string[] imageFiles = Directory.GetFiles(folderPath, "*.jpg");

        if (imageFiles.Length > 0)
        {
            string firstImageFileName = Path.GetFileName(imageFiles[0]);
            Console.WriteLine($"Name of the first image in the folder: {firstImageFileName}");
            return firstImageFileName;
        }
        else
        {
            Console.WriteLine("No images found in the folder.");
            return string.Empty;
        }
    }
}
