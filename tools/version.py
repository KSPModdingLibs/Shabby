# This file generates versioned files for deploying Shabby

import yaclog
import yaclog.version
import git as gp
import os
from pathlib import Path
from jinja2 import Environment, FileSystemLoader, select_autoescape


def run():
    basedir = Path(__file__).parent.parent

    g = gp.Git(basedir)

    release = True
    tag, distance, sha = g.execute(["git", "describe"]).split("-")

    if int(distance) > 0:
        release = False
        tag = yaclog.version.increment_version(tag, 2)

    segments = tag.split('.')
    ver_major, ver_minor, ver_patch = segments[0:3]
    if len(segments) >= 4:
        ver_build = segments[3]
    elif release:
        ver_build = int(distance)
    else:
        ver_build = int("0x" + sha[1:5], 16)

    print(f'Configuring version {ver_major}.{ver_minor}.{ver_patch} build {ver_build}')

    env = Environment(
        loader=FileSystemLoader(basedir / "Templates"),
        autoescape=select_autoescape()
    )

    for template_name in ["Assets/Shabby.version", "Source/assembly/AssemblyInfo.cs"]:
        print("Generating " + template_name)
        template = env.get_template(template_name)
        with open(basedir / template_name, "w") as fh:
            fh.write(template.render(
                ver_major=ver_major,
                ver_minor=ver_minor,
                ver_patch=ver_patch,
                ver_build=ver_build,
                tag=tag
            ))

    print('Done!')


if __name__ == '__main__':
    run()
