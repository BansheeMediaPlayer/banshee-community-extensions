import os.path
import SCons.Builder
import SCons.Node.FS
import SCons.Util

csccom = "$CSC $CSCFLAGS $_CSCLIBPATH $_CSCLIBS -out:${TARGET.abspath} $SOURCES"
csclibcom = "$CSC -t:library $CSCLIBFLAGS $_CSCLIBPATH $_CSCLIBS -out:${TARGET.abspath} $SOURCES"


McsBuilder = SCons.Builder.Builder(action = '$CSCCOM',
                                   source_factory = SCons.Node.FS.default_fs.Entry,
                                   suffix = '.exe')

McsLibBuilder = SCons.Builder.Builder(action = '$CSCLIBCOM',
                                   source_factory = SCons.Node.FS.default_fs.Entry,
                                   suffix = '.dll')

def generate(env):
    env['BUILDERS']['CLIProgram'] = McsBuilder
    env['BUILDERS']['CLILibrary'] = McsLibBuilder

    env['CSC']        = 'gmcs'
    env['_CSCLIBS']    = "${_stripixes('-r:', CILLIBS, '', '-r', '', __env__)}"
    env['_CSCLIBPATH'] = "${_stripixes('-lib:', CILLIBPATH, '', '-r', '', __env__)}"
    env['CSCFLAGS']   = SCons.Util.CLVar('')
    env['CSCCOM']     = SCons.Action.Action(csccom)
    env['CSCLIBCOM']  = SCons.Action.Action(csclibcom)

def exists(env):
    return env.Detect('gmcs')

