all: edn8usb

edn8usb.exe: Program.cs
	mcs -out:edn8usb.exe Program.cs

edn8usb: edn8usb.exe
	mkbundle -o edn8usb --simple edn8usb.exe --machine-config /etc/mono/4.5/machine.config --config /etc/mono/config
