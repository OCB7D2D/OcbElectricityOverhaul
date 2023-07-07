@echo off

REM call npm install prettier @prettier/plugin-xml
call npm exec prettier -- --plugin @prettier/plugin-xml --write "Config/**/*.xml"

pause