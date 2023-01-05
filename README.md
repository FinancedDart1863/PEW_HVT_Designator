# PEW_HVT_Detector - 6435768312

<!-- GETTING STARTED -->
## Phobos Engineered Weaponry - High Value Target (HVT) Detector

This implements a detection subsystem that scans grids within the game world and indentifies those
above a certain block size.

Customizable parameters:
- The HVT grid block size threshold

The grid scan interval is fixed at 60 minutes. Every 60 minutes, all grids 


Upon designation as a HVT, the following occurs to the grid.
- All beacons are autocompleted instantly
- All beacons are transferred to SPRT ownership
- All beacons have broadcast range set to 200km
- All beacons are enabled
- All beacons have hud text set to "HIGH VALUE TARGET"
- The grid is turned on if it is off
- All battery blocks are turned on


If a grid is a subgrid or has subgrids, the entire grid group is obtained and a cummulative sum
is computed using the block counts of all grids within the grid group.

<!-- CONTACT -->
## Contact

FinancedDart - FinancedDart#1863

Project Link: 

<p align="right">(<a href="#readme-top">back to top</a>)</p>

