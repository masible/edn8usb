all:
	make -C edn8usb/

install:
	install -m0755 edn8usb/edn8usb ~/.local/bin
