These files enable the creation of .map files that work with the bsp libraries herein.

Defaults.qrk is a snippet that should be pasted into your quark-install-dir/addons/Defaults.qrk in the
section that has all of the game specific stuff in it.  The 'Code' value is some sort of internal quark
value.  I took F as it is the next free value in the list (see GameCodeList.txt in quark-install-dir).

Defaults might need a game specific version if the game uses different default textures etc.

The rest of the files go in quark-install-dir/addons/GameName where GameName is whatever your game's name
is.  The default is GrogLibs.

DataGrogLibs is set up with basic engine flags, but all of the User flags are game specific, so every new
game will probably want to modify those to whatever is needed (clouds, fog, acid, regional lighting etc)

GrogLibsEntities is very game specific.  The default file contains a chopped down version of quark's
quake2 entities with a few fixes to flag values.  It is mostly empty.

GrogLibsTextures is a game specific list of textures.  To get this to work, you'll need to aim quark's
game directory at your Content/Textures folder.  The tricksy bit with this is that quark tries to be
smart and look for an exe in the folder.  So just make a quake2.exe out of an empty text file or
something and put it in the textures folder to make quark happy.

UserData GrogLibs contains the default cube and light.  No need to mess with it really.