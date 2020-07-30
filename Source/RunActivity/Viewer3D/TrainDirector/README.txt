Go to Package Manager Console and type
  Install-Package System.Text.Json -Version 4.7.2

Right-click on RunActivity in the Solution Explorer, select Add... > Reference... and make sure
  System.Net.Http
is checked.

DESIGN

The TDConnectorView shows a form where the player can interact
with a running instance of Train Director via TD's web API.
The form embeds a WebBrowser control that is used to render
TD's layout (tracks, signals, etc.). The rendering uses an
HTML canvas element, and is implemented in JavaScript.
This is the same code used in TD's web interface, and could
be ported to OR with minimal changes.
Of course if one would take the time to re-implement the JavaScript
code in C#, then there would be no need for the web browser control.

The form and the web browser are controlled by the TDController class
which is the bridge between TD and OR. It intercepts events in the
web browser control, interprets them and sends them to either TD or OR.
It also maps TD's layout elements (switches, signals) to OR's.
The mapping is based on TD's element "names" that encode OR's world coords.

