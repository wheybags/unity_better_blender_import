using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public class BetterBlenderImport
{
    static BetterBlenderImport()
    {
        EditorApplication.update += forwardErrorMessages;

        // If you need to attach a debugger, uncomment this to run from update instead
        // of running directly from this static constructor. For normal use though,
        // running directly is needed, as it ensures we run before mesh importing.
        // EditorApplication.update += () =>
        // {
        //     if (done)
        //         return;
        //     done = true;
        //     doPatchIfNeeded();
        // };

        doPatchIfNeeded();
    }

    static void forwardErrorMessages()
    {
        string errorsDirectory = Path.GetDirectoryName(Application.dataPath) + "/blender_to_fbx_errors";

        string[] files = null;
        try
        {
            files = Directory.GetFiles(errorsDirectory);
        }
        catch (Exception)
        {
            return;
        }

        foreach (string errorPath in files)
        {
            try
            {
                Debug.LogError(File.ReadAllText(errorPath));
                File.Delete(errorPath);
            }
            catch (Exception) {}
        }
    }

    static void doPatchIfNeeded()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
            throw new Exception("BetterBlenderImport only works on Windows");

        string requiredPath = Application.dataPath.Replace("/", "\\") + "\\Scripts\\Editor\\BetterBlenderImport.cs";
        if (!File.Exists(requiredPath))
            throw new Exception("BetterBlenderImport.cs must be placed at " + requiredPath);

        string blenderFbxPath = (EditorApplication.applicationContentsPath + "/Tools/Unity-BlenderToFBX.py").Replace("/", "\\");

        string currentSource = File.ReadAllText(blenderFbxPath);

        if (currentSource == patchedSource)
            return;
        bool isOriginal = currentSource == originalSource;

        if (isOriginal || currentSource.StartsWith(patchedSigil))
        {
            Debug.Log("BetterBlenderImport: applying patch to " + blenderFbxPath);

            string tempFolder = "C:\\UnityBetterBlenderImportTemp";
            Directory.CreateDirectory(tempFolder);

            try
            {
                string tempPyPath = tempFolder + "\\temp.py";
                string tempBatPath = tempFolder + "\\temp.bat";

                string batFileData = "";
                if (isOriginal)
                {
                    string originalPath = (EditorApplication.applicationContentsPath + "/Tools/Unity-BlenderToFBX-orig.py").Replace("/", "\\");
                    batFileData += "copy /y \"" + blenderFbxPath + "\" \"" + originalPath + "\" || goto :error\r\n";
                }

                batFileData += "copy /y \"" + tempPyPath + "\" \"" + blenderFbxPath + "\" || goto :error\r\n";
                batFileData += "exit /b 0\r\n";
                batFileData += ":error\n";
                batFileData += "set err=%errorlevel%\r\n";
                batFileData += "PAUSE\r\n";
                batFileData += "exit /b %err%\r\n";

                File.WriteAllText(tempPyPath, patchedSource);
                File.WriteAllText(tempBatPath, batFileData);

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = @"cmd.exe";
                psi.Verb = "runas"; //This is what actually runs the command as administrator
                psi.Arguments = "/C \"" + tempBatPath + "\"";

                var process = new Process();
                process.StartInfo = psi;
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new Exception("BetterBlenderImport: patch failed!");
            }
            finally
            {
                Directory.Delete(tempFolder, true);
            }
        }
        else
        {
            throw new Exception("BetterBlenderImport: " + blenderFbxPath + " contains unexpected code, cannot patch");
        }
    }

    private const string patchedSigil = "# patched by BetterBlenderImport.cs\n";

    private static readonly string patchedSource = patchedSigil + @"
# This will delegate the blender export to Assets/Scripts/Editor/Unity-BlenderToFBX-override.py if it exists.
# Otherwise, it will run the original code. This way you can do whatever custom project-specific stuff you
# want, and it won't break other unity projects.

import os
import importlib.util
import sys
import traceback
import shutil
import uuid

def import_by_path(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module

project_path = os.getcwd()

in_project_path = os.path.join(project_path, ""Assets"", ""Scripts"", ""Editor"", ""Unity-BlenderToFBX-override.py"")
orig_path = os.path.join(os.path.dirname(__file__), ""Unity-BlenderToFBX-orig.py"")

if not os.path.exists(in_project_path):
    import_by_path(""unity_blender_to_fbx"", orig_path)
else:
    try:
        temp = sys.dont_write_bytecode
        sys.dont_write_bytecode = True
        import_by_path(""unity_blender_to_fbx"", in_project_path)
        sys.dont_write_bytecode = temp

    except Exception as e:
        tb = traceback.format_exc()

        filename = '(unknown)'
        try:
            filename = sys.argv[sys.argv.index('-b') + 1]
        except:
            pass

        errors_directory = os.path.join(project_path, 'blender_to_fbx_errors')
        os.makedirs(errors_directory, exist_ok=True)
        err_path = os.path.join(errors_directory, str(uuid.uuid4()) + '.txt')
        with open(err_path, 'w') as f:
            f.write('error importing file: ' + filename + '\n\n' + tb)

        raise e
".Replace("\r\n", "\n");

    private static readonly string originalSource = @"import bpy
blender249 = True
blender280 = (2,80,0) <= bpy.app.version

try:
    import Blender
except:
    blender249 = False

if not blender280:
    if blender249:
        try:
            import export_fbx
        except:
            print('error: export_fbx not found.')
            Blender.Quit()
    else :
        try:
            import io_scene_fbx.export_fbx
        except:
            print('error: io_scene_fbx.export_fbx not found.')
            # This might need to be bpy.Quit()
            raise

# Find the Blender output file
import sys
argv = sys.argv
outfile = ' '.join(argv[argv.index(""--"") + 1:])

# Do the conversion
print(""Starting blender to FBX conversion "" + outfile)

if blender280:
    import bpy.ops
    bpy.ops.export_scene.fbx(filepath=outfile,
        check_existing=False,
        use_selection=False,
        use_active_collection=False,
        object_types= {'ARMATURE','CAMERA','LIGHT','MESH','OTHER','EMPTY'},
        use_mesh_modifiers=True,
        mesh_smooth_type='OFF',
        use_custom_props=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        apply_scale_options='FBX_SCALE_ALL')
elif blender249:
    mtx4_x90n = Blender.Mathutils.RotationMatrix(-90, 4, 'x')
    export_fbx.write(outfile,
        EXP_OBS_SELECTED=False,
        EXP_MESH=True,
        EXP_MESH_APPLY_MOD=True,
        EXP_MESH_HQ_NORMALS=True,
        EXP_ARMATURE=True,
        EXP_LAMP=True,
        EXP_CAMERA=True,
        EXP_EMPTY=True,
        EXP_IMAGE_COPY=False,
        ANIM_ENABLE=True,
        ANIM_OPTIMIZE=False,
        ANIM_ACTION_ALL=True,
        GLOBAL_MATRIX=mtx4_x90n)
else:
    # blender 2.58 or newer
    import math
    from mathutils import Matrix
    # -90 degrees
    mtx4_x90n = Matrix.Rotation(-math.pi / 2.0, 4, 'X')

    class FakeOp:
        def report(self, tp, msg):
            print(""%s: %s"" % (tp, msg))

    exportObjects = ['ARMATURE', 'EMPTY', 'MESH']

    minorVersion = bpy.app.version[1];
    if minorVersion <= 58:
        # 2.58
        io_scene_fbx.export_fbx.save(FakeOp(), bpy.context, filepath=outfile,
            global_matrix=mtx4_x90n,
            use_selection=False,
            object_types=exportObjects,
            mesh_apply_modifiers=True,
            ANIM_ENABLE=True,
            ANIM_OPTIMIZE=False,
            ANIM_OPTIMIZE_PRECISSION=6,
            ANIM_ACTION_ALL=True,
            batch_mode='OFF',
            BATCH_OWN_DIR=False)
    else:
        # 2.59 and later
        kwargs = io_scene_fbx.export_fbx.defaults_unity3d()
        io_scene_fbx.export_fbx.save(FakeOp(), bpy.context, filepath=outfile, **kwargs)
    # HQ normals are not supported in the current exporter

print(""Finished blender to FBX conversion "" + outfile)
".Replace("\r\n", "\n");
}
