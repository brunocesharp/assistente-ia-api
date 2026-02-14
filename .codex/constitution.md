Organização de pastas no projeto
ASSISTENTE-IA-API
    - Presentation
    - Application
        use-cases/
        commands/
        queries/
        dto/
        mappers/
        ports/
            in/                 # interfaces para entrada (ex: handlers)
            out/                # interfaces para saída (ex: gateway, publisher)
        services/             # serviços de aplicação (orquestração)
        validators/
    - Infrastructure
        infrastructure/
        persistence/
            orm/
            migrations/
            repositories/       # implements domain/application ports
        messaging/
            producers/
            brokers/
        http/
            server/
            middlewares/
        config/
        logging/
        monitoring/
        di/                   # dependency injection composition root
    - Interfaces/
        http/
            controllers/
            presenters/
            routes/
            request-models/
            response-models/
        cli/
        messaging/
            consumers/
        graphql/    
    - domain/
        entities/
        value-objects/
        aggregates/
        events/
        services/
        repositories/        # interfaces (ports)
        specifications/
        policies/
        exceptions/
