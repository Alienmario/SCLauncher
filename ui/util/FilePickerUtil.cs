using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace SCLauncher.ui.util;

public static class FilePickerUtil
{
	public static async Task<string?> PickFolder(Visual? visual, string title, string? start)
	{
		return await PickFolder(TopLevel.GetTopLevel(visual), title, start);
	}
	
	public static async Task<string?> PickFolder(TopLevel? topLevel, string title, string? start)
	{
		if (topLevel == null)
			return null;

		IStorageFolder? startFolder = null;
		if (start != null)
		{
			try
			{
				startFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(start));
			} catch (FormatException) {}
		}

		var files = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = title,
			AllowMultiple = false,
			SuggestedStartLocation = startFolder
		});

		return files.Count >= 1 ? files[0].Path.LocalPath : null;
	}
}