# GitTkcvsVersionTree
Git graphical branch layout approx the way it is done by tkcvs

I am not satisfied with the way most Git GUI's show the branch history layout.

Lately I ended up with a master, hotfix, develop and 2 release product branches.
The develop branch contained a lot of new functionality.
The 2 product release branches contained a lot of improvements etc, while the develop branch lags a lot behind.
Once the product release branches were ready, merging them into develop ended up in removing the new
functionality from the develop branch.
Eventually I had to cherry pick the commits from the release branches to get a decent develop branch.

The rootcause of this problem was that a colleague branched new functionality from a wrong root branch by accident due
to inexperience with git, and not a clear overview how the software was branched in git.

GitVersionTree shows a more understandable layout of the software, but it is less intuitive to me, eg
starting at oldest at the top (not like a Tree is growing).

