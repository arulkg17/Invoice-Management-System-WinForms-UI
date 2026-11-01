// One place for the namespaces every file needs (replaces the IDE's "implicit usings").
// "HttpClient", "HttpMethod", "Task", "List<>", "Color", "Button" ... just work in every class.
global using System;
global using System.Collections.Generic;
global using System.Drawing;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Windows.Forms;

// System.Threading.Timer and System.Windows.Forms.Timer clash, so we always use this alias for the UI timer.
global using WinTimer = System.Windows.Forms.Timer;
