# GrogLibs
This is a set of libraries I started on back in 2008 (I think).  I've had them in various forms here on GitHub for years, but the history was never quite correct.

The project started out I believe in perforce, then at some point transitioned to SVN, then around maybe 2015ish to mercurial, and now into git.  In the process of all this conversion the tree got really confusing and odd.  Some of the branches did some very strange things with dates.  And often renames were processed as deletion + add new file, thus losing the history.

I looked into various ways of repairing the repo through tools like git_filter_repo, but I ended up doing all of this manually, only using git_filter_repo to repair the commit dates.  This turned out to be a very time consuming task.

There's a lot of interesting history in there, particularly choices made with regards to portalization and clustering and trying to manage the various contents types of valve / quake1 / quake2 / genesis maps.  You should be able to see all the way back to my initial very basic stuff.

Way way back, there is a bit of toolish code mixed in.  Many of the things that end up in the libs started life in a tool or game project and were migrated out.

I wanted this to be just-the-libs to have this as a submodule to tools or games or whatever.  For the tools and test stuff that goes with this, I'm going to try an automated way of extracting them, as the history of those is less important to preserve (and this one took DAYS).

See LibDocs.txt for details on how stuff works.  Also the ReadMe.txt under QuArKAddon for info on setting up quake army knife for map editing.

#Building
There's a rather annoying dependency step here.  The released nuget packages for SharpDX have some breaking bugs for the stuff I use (rawinput, and 11 effects).  I've cloned it as a submodule of my fork, but it only builds for me from command line.  So it needs to have build.cmd ran or the projects will fail to find the assemblies.

I've removed the universal stuff from my fork as it was impossible to build for me with that stuff in there.  The requirements can be a bit steep even without the universal's 11 gigs.  Two different windows sdks are needed for some reason.

Eventually I'll get around to converting over to Vulkan.
