# GrogLibs
This is a set of libraries I started on back in 2008 (I think).  I've had them in various forms here on GitHub for years, but the history was never quite correct.

The project started out I believe in perforce, then at some point transitioned to SVN, then around maybe 2015ish to mercurial, and now into git.  In the process of all this conversion the tree got really confusing and odd.  Some of the branches did some very strange things with dates.  And often renames were processed as deletion + add new file, thus losing the history.

I looked into various ways of repairing the repo through tools like git_filter_repo, but I ended up doing all of this manually, only using git_filter_repo to repair the commit dates.  This turned out to be a very time consuming task.

There's a lot of interesting history in there, particularly choices made with regards to portalization and clustering and trying to manage the various contents types of valve / quake1 / quake2 / genesis maps.  You should be able to see all the way back to my initial very basic stuff.

Way way back, there is a bit of toolish code mixed in.  Many of the things that end up in the libs started life in a tool or game project and were migrated out.

I wanted this to be just-the-libs to have this as a submodule to tools or games or whatever.  For the tools and test stuff that goes with this, I'm going to try an automated way of extracting them, as the history of those is less important to preserve (and this one took DAYS).

See LibDocs.txt for details on how stuff works.  Also the ReadMe.txt under QuArKAddon for info on setting up quake army knife for map editing.

# Building

I'm switching from SharpDX to Vortice for my DirectX layer stuff.  This makes building much less painful.  Individual projects should build just fine with dotnet build, or the whole solution.

Things are however quite broken as of this change.  I am slowly fixing and rewiring things to get it all working again.

The most major change so far is the elimination of the dependency on the effect framework.  This has made the concept of a material a bit more challenging to deal with.  As such material related things will be alot less generic.  I won't be able to just connect a datagrid to a list of materials and have it just work.

The coordinate system stays the same but anything passed to directX needs a transpose.  The effect framework was handling this for me without me even realizing it, so I'm still adjusting to that.