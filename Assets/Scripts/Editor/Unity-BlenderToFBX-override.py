import bpy
import bpy.ops
import sys

# This basic version was copied from the original, but I removed the code
# for handling ancient blender versions, to make it easier to understand
# what's actually happening.
#
# If this file throws exceptions at all, they will be shown in a console
# window. print()s will unfortunately not show up in the unity console,
# so if you need to debug something, you can either write text to a file,
# or just run this code directly in the blender python console.

infile = filename = sys.argv[sys.argv.index('-b') + 1] # you can customise based on this if you like
outfile = ' '.join(sys.argv[sys.argv.index("--") + 1:])

bpy.ops.export_scene.fbx(filepath=outfile,
    check_existing=False,
    use_selection=False,
    use_active_collection=False,
    object_types={'ARMATURE','CAMERA','LIGHT','MESH','OTHER','EMPTY'},
    use_mesh_modifiers=True,
    mesh_smooth_type='OFF',
    use_custom_props=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=False,
    apply_scale_options='FBX_SCALE_ALL')
