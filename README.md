# WebAppMVC — Leave Management System (API Version)

An ASP.NET Core MVC web application for managing employee leave requests, built with a two-stage approval workflow (Manager → HR). This is the client-facing application, refactored to consume a separate backend API instead of accessing the database directly — reflecting a real-world, API-first architecture.

## Overview

This project started as a traditional MVC app with direct database access, and was rearchitected to communicate exclusively through [WebInterface](../WebInterface) — a dedicated Web API — via `HttpClient`. This separation means the same backend could just as easily serve a mobile app or another client in the future, without any changes to the API itself.

## Tech Stack

- **ASP.NET Core MVC** (.NET 10)
- **ASP.NET Core Identity** — cookie-based session management
- **JWT** — authentication token issued by the API, carried via cookie and attached to outgoing requests automatically
- **QuestPDF** — PDF report generation
- **MailKit** — invite emails
- **Bootstrap 5** — UI, with a custom design system (CSS variables for consistent theming)

## Key Features

- **Employee Dashboard** — leave balances, leave history, and role-specific widgets (Manager/HR overviews)
- **Employee Directory** — HR can view, create, edit, and manage employee records
- **Leave Application** — employees apply for leave with optional file attachments (e.g. medical certificates)
- **Two-stage Approval Workflow** — Manager approves first, then HR gives final approval; leave balances update automatically on approval
- **Invite-based Onboarding** — HR invites employees by email; users set their own password to activate their account
- **Leave Summary Report** — HR-only report showing leave usage across all employees, exportable as a PDF
- **Role-based Access Control** — Employee, Manager, and HR roles with distinct permissions throughout the app

## Architecture Highlight

Rather than using Entity Framework directly in controllers, every data operation goes through the API:
Browser → MVC Controller → HttpClient (with JWT) → WebInterface API → Database

A custom `DelegatingHandler` (`AuthTokenHandler`) automatically attaches the logged-in user's JWT token to every outgoing API request, so individual controllers never have to manage authentication headers manually.

## Getting Started

1. Ensure the [WebInterface](../WebInterface) API is running (this app depends on it for all data)
2. Update the API base URL and connection string in `appsettings.json`
3. Configure the cookie authentication and Identity settings as needed
4. Run the project — login with a seeded test account to get started

## Project Structure Notes

- `Handlers/AuthTokenHandler.cs` — attaches JWT tokens to outgoing HttpClient requests
- `PdfDocuments/` — QuestPDF document definitions for report generation
- Custom theming is defined via CSS variables in `_Layout.cshtml` for a consistent look across all pages
