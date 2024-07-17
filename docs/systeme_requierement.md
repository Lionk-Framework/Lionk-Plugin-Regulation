### Specifications Document for the Fireplace Accumulation Regulation System

#### 1. Introduction
The goal of this project is to develop a regulation system for a 29 [kW] accumulation fireplace. This system should regulate the temperature, transfer the produced energy to two thermal accumulators, and provide various features to enhance performance and usability.

#### 2. Project Objectives
- Migrate the regulation system from Python to .NET.
- Avoid abrupt temperature variations in the fireplace.
- Provide secure data access from anywhere via a web platform.
- Notify the user in case of system failure or overheating.
- Enable automatic program startup on Raspberry Pi boot.
- Ensure secure communication between mobile widgets and the installation.
- Implement a monitoring and logging system.
- Implement an automatic software component update system.

#### 3. Current Installation
The current system runs on a `Raspberry Pi 4B 4Gb` with code written in `Python 3.9`. The circulation pump activates based on the fireplace temperature and stops when the temperature drops below a defined threshold. Data is accessible in real-time via VNC Viewer.

#### 4. Current Issues
1. **Lack of Regulation**: Abrupt temperature variations due to the introduction of cold water.
2. **Data Access**: Users must connect to the Raspberry Pi desktop interface via VNC Viewer.
3. **System Startup**: The system does not automatically start after a Raspberry Pi reboot.
4. **Notifications**: No notification system for energy transport failure or overheating.

#### 5. Functional Requirements
1. **Temperature Regulation**:
   - The system should regulate the fireplace and thermal accumulator temperatures.
2. **Secure Data Access**:
   - Data must be accessible in real-time via a secure web interface.
3. **Notifications**:
   - Send notifications in case of energy transport system failure or overheating.
4. **Automatic Startup**:
   - The program must start automatically when the Raspberry Pi boots.
5. **Secure Communication**:
   - Ensure secure communication between mobile widgets and the installation via a GRPC protocol.
6. **Monitoring and Logging**:
   - Implement a monitoring system to oversee system performance.
   - Log data for performance tracking and troubleshooting.
7. **Automatic Updates**:
   - Use GitHub Actions for continuous updates of software components.

#### 6. Non-Functional Requirements
1. **Performance**:
   - The system should respond quickly to temperature changes.
2. **Security**:
   - Data must be protected and accessible only by authorized users.
   - Communications should be encrypted.
3. **Reliability**:
   - The system should be reliable and capable of continuous operation without major interruptions.
4. **Scalability**:
   - The system should be scalable to integrate new components or features.
5. **Maintenance**:
   - The code should be well-documented and tested for ease of maintenance and future updates.
6. **Compatibility**:
   - The system should be compatible with .NET 8.0 or higher, Blazor, ASP.NET Core, Entity Framework Core, and Docker.

#### 7. Proposed Solution
- **.NET Program on Raspberry Pi**:
  - Read and write data on GPIOs to control the installation.
  - Regulate temperature based on collected data.
  - Notify users in case of failure or overheating.
- **API Server on Raspberry Pi**:
  - Handle HTTP requests with ASP.NET Core and authentication.
- **Database on Linux Server**:
  - Store time-series data with Entity Framework Core.
- **Web Server on Raspberry Pi or Linux Server**:
  - Allow real-time and delayed data access via a secure web interface.
- **Notification System**:
  - Use an external service to send notifications in case of failure or overheating.
- **Automatic Updates**:
  - Use GitHub Actions for automatic updates of software components.

#### 8. Current Installation Diagram
Mettre diagramme de l'installation

#### 9. Conclusion
This project aims to improve the regulation of an accumulation fireplace by providing advanced features for temperature management, data access, and issue notifications. The transition to .NET and the integration of new technologies will ensure a more reliable, secure, and easy-to-maintain system.

#### 10. Appendices
mettre les annexes
- Technical Documentation
- Source Code
