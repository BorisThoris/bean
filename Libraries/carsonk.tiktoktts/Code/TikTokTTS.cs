using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;

namespace TikTokTTS;

/// <summary>
/// A static class that allows you to use the TikTokTTS API.
/// </summary>
public static class TikTokTTS
{
    /// <summary>
    /// The API endpoint that the library uses.
    /// </summary>
    public static string ENDPOINT = "https://tiktok-tts.weilnet.workers.dev";

    static int currentId = 0;

    /// <summary>
    /// Use TikTok's TTS to speak via a MusicPlayer. This MusicPlayer will automatically dispose itself when it finishes playing.
    /// </summary>
    /// <param name="text">The text to be said by the TTS</param>
    /// <param name="voice">The name of the voice</param>
    /// <returns></returns>
    public static async Task<MusicPlayer> Say( string text, string voice = "en_us_007" )
    {
        string fileName = $"tts-{currentId}.mp3";
        currentId++;

        if ( !FileSystem.Data.FileExists( fileName ) )
        {
            await Download( text, voice, fileName, FileSystem.Data );
        }

        var musicPlayer = MusicPlayer.Play( FileSystem.Data, fileName );
        musicPlayer.ListenLocal = true;

        return musicPlayer;
    }

    /// <summary>
    /// Use TikTok's TTS to speak and download/save the mp3 file.
    /// </summary>
    /// <param name="text">The text to be said by the TTS</param>
    /// <param name="voice">The name of the voice</param>
    /// <param name="fileName">The file name/path it should download to (defaults to "tts-##########.mp3"</param>
    /// <param name="fileSystem">The filesystem to use (defaults to FileSystem.Data)</param>
    /// <returns>The file path</returns>
    public static async Task<string> Download( string text, string voice = "en_us_007", string fileName = null, BaseFileSystem fileSystem = null )
    {
        if ( fileSystem == null ) fileSystem = FileSystem.Data;
        if ( string.IsNullOrEmpty( fileName ) ) fileName = $"tts-{new Guid()}.mp3";

        var content = new Dictionary<string, string> {
            { "text", text },
            { "voice", voice }
        };
        var headers = new Dictionary<string, string> {
            { "Content-Type", "application/json" }
        };

        text = text.RemoveBadCharacters();
        text = text.Substring( 0, Math.Min( text.Length, 300 ) );
        if ( !text.Any( x => char.IsLetterOrDigit( x ) ) ) return null;
        if ( string.IsNullOrEmpty( text ) ) return null;

        // Make sure folders exist
        var folders = fileName.Split( '/' );
        if ( folders.Length == 1 ) folders = fileName.Split( '\\' );
        if ( folders.Length > 1 )
        {
            string path = "";
            for ( int i = 0; i < folders.Length - 1; i++ )
            {
                path += folders[i] + "/";
                if ( !fileSystem.DirectoryExists( path ) )
                    fileSystem.CreateDirectory( path );
            }
        }

        try
        {
            if ( fileSystem.FileExists( fileName ) )
                fileSystem.DeleteFile( fileName );
            var response = await Http.RequestJsonAsync<TikTokTTSResponse>( "https://tiktok-tts.weilnet.workers.dev/api/generation", "POST", Http.CreateJsonContent( content ), headers );
            string base64 = response.data;
            byte[] mp3 = Convert.FromBase64String( base64 );
            var stream = fileSystem.OpenWrite( fileName );
            stream.Write( mp3, 0, mp3.Length );
            stream.Close();
            return fileName;
        }
        catch ( Exception e )
        {
            Log.Error( e.Message );
            return null;
        }
    }

    class TikTokTTSResponse
    {
        public bool success { get; set; }
        public string data { get; set; }
        public string error { get; set; }
    }
}