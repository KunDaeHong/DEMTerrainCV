#if UNITY_STANDALONE_WIN

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Ookii.Dialogs;

using UnityEngine;

public class WindowWrapper : IWin32Window
{
    private IntPtr _hwnd;
    public WindowWrapper(IntPtr handle) { _hwnd = handle; }
    public IntPtr Handle { get { return _hwnd; } }
}
public class FileUtilWindows : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    public string[] filePickerPanel(string title, string dir, ExtensionFilter[] extensionFilters, bool multiSelect)
    {
        var fd = new VistaOpenFileDialog();
        fd.Title = title;
        if (extensionFilters != null)
        {
            fd.Filter = GetFilterFromFileExtensionList(extensionFilters);
            fd.FilterIndex = 1;
        }
        else
        {
            fd.Filter = string.Empty;
        }

        fd.Multiselect = multiSelect;

        if (!string.IsNullOrEmpty(dir))
        {
            fd.FileName = getDirPath(dir);
        }

        var res = fd.ShowDialog(new WindowWrapper(GetActiveWindow()));

        var fileNames = res == DialogResult.OK ? fd.FileNames : new string[0];
        fd.Dispose();

        return fileNames;
    }

    private static string GetFilterFromFileExtensionList(ExtensionFilter[] extensions)
    {
        var filterString = "";

        foreach (var filterFile in extensions)
        {
            filterString += filterFile.Name + "(";

            foreach (var ext in filterFile.Extensions)
            {
                filterString += "*." + filterFile + ";";
            }

            filterString = filterString.Remove(filterString.Length - 1);
            filterString += ") |";

            foreach (var ext in filterFile.Extensions)
            {
                filterString += "*." + ext + "; ";
            }

            filterString += "|";
        }

        filterString = filterString.Remove(filterString.Length - 1);
        return filterString;
    }

    public void openFilePickerPanelAsync(string title, string dir, ExtensionFilter[] extensionFilters, bool multiSelect, Action<string[]> cb)
    {
        cb.Invoke(filePickerPanel(title, dir, extensionFilters, multiSelect));
    }

    private static string getDirPath(string dir)
    {
        var dirPath = Path.GetFullPath(dir);

        if (!dirPath.EndsWith("\\"))
        {
            dirPath += "\\";
        }

        if (Path.GetPathRoot(dirPath) == dirPath)
        {
            return dirPath;
        }

        return Path.GetDirectoryName(dirPath) + Path.AltDirectorySeparatorChar;
    }
}

#endif

public struct ExtensionFilter
{
    public string Name;
    public string[] Extensions;

    public ExtensionFilter(string filterName, params string[] filterExtensions)
    {
        Name = filterName;
        Extensions = filterExtensions;
    }
}