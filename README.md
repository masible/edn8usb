**Q: How to load rom for test ?**

A: `edn8usb testrom.nes`

**Q: How to load rom with custom fpga mapper for test ?**

A: `edn8usb edfc-fpga.rbf testrom.nes`

**Q: Where i can get drivers for usb port?**

A: http://www.ftdichip.com/FTDrivers.htm

**Q: Can i use USB port to transfer some debug data from nes to PC?**

A: Yes

1. `lda #1, sta $4400, lda #3, sta $4402 //magic initialization`
2. now we can communicate with PC via `$4401`
also can be usefull to check `$4403.1` and `$4403.2` before than read or write to `$4401`.

`$4403.1` shows if port ready for read

`$4403.2` shows if port ready for write

fpga `bus_mode` should be equal to `3(BMOD_REGS)`, otherwise access to custom registers will be closed.

From PC side communication interface looks like standard serial COM port.

Contact me if you have some other questions about this stuff

biokrik@gmail.com

http://krikzz.com/forum/

07.02.2013

end
