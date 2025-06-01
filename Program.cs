using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SharpHero;

/// <summary>
///     The <see cref="Program" /> class serves as the entry point.
/// </summary>
public static unsafe class Program
{
    /// <summary>
    ///     Represents the name of the window class used to define the application window.
    ///     This name is associated with the window class during its registration and is referenced
    ///     when creating the main application window.
    /// </summary>
    private const string WindowClassName = "SharpHeroWndClass";

    /// <summary>
    ///     Specifies the title text for the main application window.
    ///     This title is displayed in the title bar of the window and represents the application.
    /// </summary>
    private const string WindowTitle = "SharpHero";

    /// <summary>
    ///     Defines the style attributes for a window class.
    ///     It determines how the windows created using this class will behave,
    ///     including their redrawing policies and device context handling,
    ///     such as enabling redraw on size or content update and using a dedicated device context for the window.
    /// </summary>
    private const WNDCLASS_STYLES WindowClassStyle = WNDCLASS_STYLES.CS_OWNDC |
                                                     WNDCLASS_STYLES.CS_HREDRAW |
                                                     WNDCLASS_STYLES.CS_VREDRAW;

    /// <summary>
    ///     Represents a delegate pointing to the window procedure callback function,
    ///     which processes messages sent to a window.
    ///     It is used to define the behavior of the window class when handling system
    ///     and application-specific messages (e.g., painting, resizing, or closing a window).
    /// </summary>
    private static readonly WNDPROC WndProc = WindowProc;


    private static void Main()
    {
        var windowInitializer = new WindowInitializationResult(InitializeInstance());

        if (!windowInitializer.TryInitialize(WindowClassName, WindowTitle, out var mainWindow)) return;

        RunMessageLoop();
    }

    /// Initializes the instance handle for the application by retrieving it using the GetModuleHandle function.
    /// This is typically used to identify the executable instance of the application.
    /// <returns>
    ///     The <see cref="HINSTANCE" /> handle of the application instance, or the default value if the operation fails.
    /// </returns>
    private static HINSTANCE InitializeInstance()
    {
        var hInstance = PInvoke.GetModuleHandle((PCWSTR)null);
        if (hInstance == default)
            LogWin32Error("GetModuleHandle");
        return hInstance;
    }

    /// Registers a window class with the Windows system, allowing the creation of windows that use this class.
    /// <param name="instance">
    ///     The handle to the instance of the application that owns the window class.
    /// </param>
    /// <param name="className">
    ///     The name of the window class being registered. This name must be unique within the application.
    /// </param>
    /// <returns>
    ///     A boolean value indicating whether the registration was successful.
    ///     Returns true if the class was successfully registered, or false if the operation failed.
    /// </returns>
    private static bool RegisterWindowClass(HINSTANCE instance, PCWSTR className)
    {
        var wndClass = new WNDCLASSW
        {
            style = WindowClassStyle,
            lpfnWndProc = WndProc,
            hInstance = instance,
            lpszClassName = className
        };

        if (PInvoke.RegisterClass(in wndClass) == 0)
        {
            LogWin32Error("RegisterClass");
            return false;
        }

        return true;
    }

    /// Creates the main application window by invoking the CreateWindowEx function.
    /// This method configures the basic attributes of the window, such as its style,
    /// position, size, and class information.
    /// <param name="instance">
    ///     The handle to the application instance, used to associate the window with the calling application.
    /// </param>
    /// <param name="className">
    ///     The name of the registered window class to associate with the new window.
    /// </param>
    /// <param name="windowName">
    ///     The title or name displayed in the title bar of the window.
    /// </param>
    /// <returns>
    ///     The <see cref="HWND" /> handle representing the created window,
    ///     or <see cref="HWND.Null" /> if the window creation fails.
    /// </returns>
    private static HWND CreateMainWindow(HINSTANCE instance, PCWSTR className, PCWSTR windowName)
    {
        var windowHandle = PInvoke.CreateWindowEx(
            WINDOW_EX_STYLE.WS_EX_LEFT,
            className,
            windowName,
            WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_VISIBLE,
            PInvoke.CW_USEDEFAULT,
            PInvoke.CW_USEDEFAULT,
            PInvoke.CW_USEDEFAULT,
            PInvoke.CW_USEDEFAULT,
            HWND.Null,
            HMENU.Null,
            instance
        );

        if (windowHandle == HWND.Null)
            LogWin32Error("CreateWindowEx");

        return windowHandle;
    }

    /// Processes and dispatches Windows messages in a loop until the application receives a quit message.
    /// This method handles the retrieval, translation, and dispatching of messages from the message queue,
    /// ensuring the application responds to user input and system messages appropriately.
    private static void RunMessageLoop()
    {
        while (PInvoke.GetMessage(out var message, HWND.Null, 0, 0))
        {
            PInvoke.TranslateMessage(in message);
            PInvoke.DispatchMessage(in message);
        }
    }

    /// Logs the details of the last Win32 error to the console with a message indicating the operation that failed.
    /// <param name="operation">
    ///     The operation associated with the error, used to provide context for the error message.
    /// </param>
    private static void LogWin32Error(string operation)
    {
        var error = Marshal.GetLastWin32Error();
        Console.WriteLine($"{operation} failed: {new Win32Exception(error).Message}");
    }

    /// Handles the WM_PAINT message by preparing the window for painting and performing basic graphics operations.
    /// Retrieves the painting area using the BeginPaint function, clears it with a solid color, and finalizes the drawing process.
    /// This helps to update the specified region of the window during the paint event.
    /// <param name="windowHandle">
    ///     The handle to the window receiving the WM_PAINT message.
    /// </param>
    /// <returns>
    ///     A default <see cref="LRESULT" /> indicating the result of processing the WM_PAINT message.
    /// </returns>
    private static LRESULT HandlePaintMessage(HWND windowHandle)
    {
        var deviceContext = PInvoke.BeginPaint(windowHandle, out var paintStruct);
        var x = paintStruct.rcPaint.left;
        var y = paintStruct.rcPaint.top;
        var width = paintStruct.rcPaint.right - x;
        var height = paintStruct.rcPaint.bottom - y;

        if (width <= 0 || height <= 0)
        {
            PInvoke.EndPaint(windowHandle, in paintStruct);
            return default;
        }

        if (deviceContext == HDC.Null)
        {
            LogWin32Error("BeginPaint");
            return default;
        }

        PInvoke.PatBlt(deviceContext, 0, 0, width, height, ROP_CODE.BLACKNESS);
        PInvoke.EndPaint(windowHandle, in paintStruct);
        return default;
    }

    /// Processes messages sent to the window procedure and provides the appropriate handling for window events such as
    /// close, paint, and others.
    /// <param name="windowHandle">
    ///     The handle to the window receiving the message.
    /// </param>
    /// <param name="message">
    ///     The message code identifying the type of message or event.
    /// </param>
    /// <param name="wParam">
    ///     Additional message information specific to the message; the meaning depends on the message sent.
    /// </param>
    /// <param name="lParam">
    ///     Additional message information specific to the message; the meaning depends on the message sent.
    /// </param>
    /// <returns>
    ///     A <see cref="LRESULT" /> value that indicates the result of the message processing.
    /// </returns>
    private static LRESULT WindowProc(HWND windowHandle, uint message, WPARAM wParam, LPARAM lParam)
    {
        return message switch
        {
            PInvoke.WM_DESTROY or PInvoke.WM_CLOSE => HandleWindowClose(),
            PInvoke.WM_PAINT => HandlePaintMessage(windowHandle),
            PInvoke.WM_ACTIVATEAPP or PInvoke.WM_SIZE => default,
            _ => PInvoke.DefWindowProc(windowHandle, message, wParam, lParam)
        };
    }

    /// Handles the window close message by initiating the application shutdown process using the PostQuitMessage function.
    /// This ensures that the application's message loop exits gracefully when the window is closed.
    /// <returns>
    ///     The default <see cref="LRESULT" /> value indicating no further processing is needed.
    /// </returns>
    private static LRESULT HandleWindowClose()
    {
        PInvoke.PostQuitMessage(0);
        return default;
    }

    /// <summary>
    ///     Represents the result of a window initialization attempt within the application.
    /// </summary>
    private readonly struct WindowInitializationResult(HINSTANCE instance)
    {
        /// Attempts to initialize a window with the specified class name and title, creating the main application window if successful.
        /// <param name="className">The name of the window class to register and use for the application window.</param>
        /// <param name="windowTitle">The title text to display on the main application window.</param>
        /// <param name="mainWindow">
        ///     Outputs the handle to the main application window if initialization is successful; otherwise,
        ///     outputs a null handle.
        /// </param>
        /// <returns>
        ///     A boolean value indicating whether the initialization was successful. Returns true if the window is successfully
        ///     initialized and created; otherwise, false.
        /// </returns>
        public bool TryInitialize(string className, string windowTitle, out HWND mainWindow)
        {
            mainWindow = HWND.Null;

            // Early exit if we don't have a valid instance handle
            if (instance == default) return false;

            fixed (char* classNamePtr = className)
            fixed (char* windowNamePtr = windowTitle)
            {
                // Convert to Windows-compatible string format, as the API expects a PCWSTR
                var classNamePcwstr = new PCWSTR(classNamePtr);

                if (!RegisterWindowClass(instance, classNamePcwstr)) return false;
                mainWindow = CreateMainWindow(
                    instance,
                    classNamePcwstr,
                    new PCWSTR(windowNamePtr));

                // Check if the main window was created successfully
                return mainWindow != HWND.Null;
            }
        }
    }
}