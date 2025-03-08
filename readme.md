# Better blender import

Unity's blender import feature is buggy and bad. Let's fix that!

There are two main issues:
- Errors are swallowed up and not displayed to the user. You just get a generic "Blender could not convert the .blend file to FBX file" message, which doesn't tell you anything about why the export failed.
- You can't customise the export process at all.

This project fixes both of these issues. When something goes wrong on import, we show the error in a console window:

IMAGE HERE

As for customisation, you can write whatever custom code you want [here](Assets/Scripts/Editor/Unity-BlenderToFBX-override.py). Your customisations are per-project, and will not affect other projects running on your machine at all.

## Installation
**Windows only!** Just copy the Assets folder on top of your project's Assets folder. Don't move the files around, they depend on their path inside Assets being left exactly as it is. The first time you open unity after this, you will get a UAC run as admin prompt. This is because we need to make a small edit to a file inside the unity installation folder, which requires admin when it's installed in the default location (`C:\Program Files`). More details below.

## How does it work?
Unity has some custom code inside the game engine for blender imports. This code runs blender with some magic params to make it load a python file `Editor/Data/Tools/Unity-BlenderToFBX.py` located inside the unity install directory. Blender has a python scripting API, and the python script uses it to export and FBX file that unity natively supports. What we do is run an editor script that modifies that file inside the unity directory. The modified script will check the Assets folder of the project that called it, and if it finds an override script there, it runs it. If it doesn't find an override, it runs a copy of the original unmodified script. This way you can do whatever custom stuff you want in your override script, and it won't break other projects because it's not installed globally.

## Limitations
- Only works on Windows (editor only, export to other platforms is not affected).
- It's kind of a hack, so it might break if unity changes how they handle blender import. That said, unity doesn't seem to have touched blender import for about a decade so we're probably OK.
- Tested on unity 2022.3.2f1 and 6000.0.41f1.
