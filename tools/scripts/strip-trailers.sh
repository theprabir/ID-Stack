#!/bin/sh
# Strips bot attribution trailers from commit messages.
# Used by: git filter-branch --msg-filter "sh <absolute-path-to-this-file>" -- --all
sed -e '/Generated with Codebuff/d' \
    -e '/Co-Authored-By: Codebuff/d'
