<a  name="readme-top"></a>

  

<!-- PROJECT SHIELDS -->

[![Stargazers][stars-shield]][stars-url]

[![MIT License][license-shield]][license-url]

  

<!-- PROJECT LOGO -->

<br />

<div  align="center">

<a  href="https://github.com/kalmix/CoreTray">

<img  src="https://ucarecdn.com/a57e2c41-6d8e-49ef-bfc6-f79a776ee163/TEMPY3.png"  alt="Logo"  width="80"  height="80">

</a>

  

<h3  align="center">CoreTray</h3>

  

<p  align="center">

Real-Time Hardware Monitor for Windows - Keep your system performance at your fingertips with CoreTray's modern, native monitoring experience.

<br />

<a  href="https://github.com/kalmix/CoreTray/releases/"><strong>Download Now »</strong></a>

</div>

  

<!-- TABLE OF CONTENTS -->

<details>

<summary>Table of Contents</summary>

<ol>

<li>

<a  href="#about-the-project">About The Project</a>

<ul>

<li><a  href="#features">Features</a></li>

<li><a  href="#built-with">Built With</a></li>

</ul>

</li>

<li>

<a  href="#getting-started">Getting Started</a>

<ul>

<li><a  href="#prerequisites">Prerequisites</a></li>

<li><a  href="#installation">Installation</a></li>

</ul>

</li>

<li><a  href="#usage">Usage</a></li>

<li><a  href="#roadmap">Roadmap</a></li>

<li><a  href="#license">License</a></li>

</ol>

</details>

  

<!-- ABOUT THE PROJECT -->

## About The Project

  

[![CoreTray Screen Shot][product-screenshot]](https://github.com/kalmix/CoreTray)

  

CoreTray is a modern, native Windows application that provides real-time monitoring of your system's hardware performance. Built with WinUI 3 and following Windows 11 design principles, CoreTray offers a beautiful, fluent interface that integrates seamlessly with your desktop.

  

Whether you're a gamer, content creator, or power user, CoreTray keeps you informed about your CPU, GPU, and RAM performance with live graphs, temperature monitoring, and customizable settings - all accessible from your system tray.

  

<p  align="right">(<a  href="#readme-top">back to top</a>)</p>

  

### Features

  

**Real-Time Monitoring**

- Live performance graphs with smooth animations

- CPU usage and temperature tracking

- GPU usage and temperature monitoring

- RAM usage percentage display

  
**Modern Design**

- Windows 11 Fluent Design with Mica backdrop

- Dark and Light theme support

- Smooth transitions and animations

- System tray integration

  

**Customizable Settings**

- Adjustable update intervals (500ms - 5000ms)

- Multiple temperature units (Celsius, Fahrenheit, Kelvin)

- Configurable decimal precision

- Graph history customization

  

**Performance Optimized**

- Minimal resource usage

- Hardware-accelerated rendering (WIN UI DOES IT NOT ME)

- Background monitoring capability

- Efficient data collection

  

<p  align="right">(<a  href="#readme-top">back to top</a>)</p>

  

### Built With

  

* [![CSharp][CSharp-badge]][CSharp-url]

* [![WinUI3][WinUI3-badge]][WinUI3-url]

* [![LiveCharts][LiveCharts-badge]][LiveCharts-url]

* [![LibreHardwareMonitor][LibreHardware-badge]][LibreHardware-url]

  

<p  align="right">(<a  href="#readme-top">back to top</a>)</p>

  

<!-- GETTING STARTED -->

## Getting Started

  

### Prerequisites

  

To run CoreTray, you need:

- Windows 10 version **1809 (October 2018 Update)** or later

- Windows 11 (recommended for best experience)

- .NET 7.0 Runtime (included with installer)

  

To build from source, you need:

- Visual Studio 2022 (17.3 or later)

- Windows App SDK 1.4 or later

- .NET 7.0 SDK

  

### Installation

  

#### Option 1: Install from Releases (Recommended)

  

1. Download the latest installer from the [Releases](https://github.com/kalmix/CoreTray/releases) page

2. Run the installer package

3. Launch CoreTray from the Start Menu

  

[**Download Latest Release »**](https://github.com/kalmix/CoreTray/releases)

  

#### Option 2: Build from Source

  

1. Clone the repository

```sh

git clone https://github.com/kalmix/CoreTray.git

```

2. Open `CoreTray.sln` in Visual Studio 2022

3. Restore NuGet packages

4. Build the solution (F6)

5. Run the project (F5)

  

<p  align="right">(<a  href="#readme-top">back to top</a>)</p>

  

<!-- USAGE -->

## Usage

  

### First Launch

  

On first launch, CoreTray will present a welcome dialog introducing you to the key features. You can:

- Navigate through the introduction using the "Next" button

- Skip the introduction anytime using the "Skip" button

- Start using the app with "Get Started"

  

### Monitoring Your Hardware

  

-  **CPU Tab**: View real-time CPU usage and temperature with a live graph

-  **GPU Tab**: Monitor GPU performance and thermal data

-  **RAM Tab**: Track memory usage percentage

  

### System Tray Integration

  

Enable system tray integration from Settings to:

- Minimize CoreTray to the system tray instead of closing

- Quick access to the app from the notification area

- Background monitoring when the window is minimized

  

### Customizing Settings

  

Access Settings to configure:

-  **Update Interval**: How frequently data is refreshed (500ms - 5000ms)

-  **Temperature Unit**: Choose between Celsius, Fahrenheit, or Kelvin

-  **Decimal Precision**: Set decimal places for temperature display (0-2)

-  **Graph Data Points**: Adjust graph history length

-  **System Tray**: Enable/disable tray integration

-  **Theme**: Choose between Light, Dark, or System theme

  

<p  align="right">(<a  href="#readme-top">back to top</a>)</p>

  

<!-- ROADMAP -->

## Roadmap

  

- [x] Real-time CPU monitoring

- [x] Real-time GPU monitoring

- [x] RAM usage tracking

- [x] Temperature monitoring (CPU & GPU)

- [x] Live performance graphs

- [x] System tray integration

- [x] Customizable settings

- [ ] Network usage monitoring

- [ ] Disk usage monitoring

- [ ] Alert notifications for high temperatures

- [ ] Export performance data

  

<p  align="right">(<a  href="#readme-top">back to top</a>)</p>

  

<!-- LICENSE -->

## License

  

Distributed under the MIT License. See `LICENSE` for more information.

  

<p  align="right">(<a  href="#readme-top">back to top</a>)</p>

  

---

  

<div  align="center">

Made with ❤️ using WinUI 3

</div>

  

<!-- MARKDOWN LINKS & IMAGES -->

[stars-shield]: https://img.shields.io/github/stars/kalmix/CoreTray.svg?style=for-the-badge

[stars-url]: https://github.com/kalmix/CoreTray/stargazers

[license-shield]: https://img.shields.io/github/license/kalmix/CoreTray.svg?style=for-the-badge

[license-url]: https://github.com/kalmix/CoreTray/blob/main/LICENSE

[product-screenshot]: https://ucarecdn.com/007204df-8da3-47ec-860a-b89620cb910d/Screenshot20251023232012.png

[CSharp-badge]: https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white

[CSharp-url]: https://dotnet.microsoft.com/en-us/languages/csharp

[WinUI3-badge]: https://img.shields.io/badge/WinUI%203-0078D4?style=for-the-badge&logo=windows&logoColor=white

[WinUI3-url]: https://learn.microsoft.com/en-us/windows/apps/winui/winui3/

[LiveCharts-badge]: https://img.shields.io/badge/LiveCharts%202-FF6384?style=for-the-badge&logo=chartdotjs&logoColor=white

[LiveCharts-url]: https://livecharts.dev/docs/winui/2.0.0-rc2/gallery

[LibreHardware-badge]: https://img.shields.io/badge/LibreHardwareMonitor-00979D?style=for-the-badge&logo=arduino&logoColor=white

[LibreHardware-url]: https://www.nuget.org/packages/LibreHardwareMonitorLib/