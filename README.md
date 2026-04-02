🧠 Surgenius is a high-performance medical-tech backend designed to manage surgical cases and medical imaging with AI-driven insights. It provides a secure, scalable ecosystem for doctors to document clinical cases, upload high-resolution scans, and receive automated diagnostic analysis to support surgical decision-making.


🏗️ Technical Architecture & Backend Stack
Built with .NET 9 following Clean Architecture (Onion Architecture) to ensure strict separation of concerns and maintainability. The core utilizes Entity Framework Core with SQL Server for data persistence, secured by a robust JWT-based Identity system with custom RBAC (Role-Based Access Control) to manage complex Doctor-Student relationships.


⚙️ Key Backend Workflows

Infrastructure: Decoupled File Storage service (Local/Cloud-ready) for handling large medical datasets.

Security: Standardized OpenID Connect claims for modern, secure authentication.

API Design: RESTful principles with a focus on Eager Loading and optimized Data Transfer Objects (DTOs) for high-speed performance.
