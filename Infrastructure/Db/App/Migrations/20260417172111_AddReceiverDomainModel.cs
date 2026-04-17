using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Db.App.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiverDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.CreateTable(
                name: "domains",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "ИД сущности.")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name_original = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Оригинальное имя домена."),
                    name_normalized = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Нормализованное имя домена."),
                    name_punycode = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Punycode представление домена."),
                    tld = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Домен верхнего уровня."),
                    sld = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "SLD часть доменного имени."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by = table.Column<string>(type: "text", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by = table.Column<string>(type: "text", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by = table.Column<string>(type: "text", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_domains", x => x.id);
                },
                comment: "Домен в нормализованном виде.");

            migrationBuilder.CreateTable(
                name: "receiver_processing_logs",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "ИД сущности.")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "MessageId сообщения."),
                    external_auction_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "Внешний идентификатор торгов."),
                    source_platform_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Код платформы."),
                    result_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Код результата обработки."),
                    message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true, comment: "Описание результата или ошибки."),
                    trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "TraceId запроса/обработки."),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Время фактической обработки."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by = table.Column<string>(type: "text", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by = table.Column<string>(type: "text", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by = table.Column<string>(type: "text", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receiver_processing_logs", x => x.id);
                },
                comment: "Лог обработки сообщения receiver-пайплайном.");

            migrationBuilder.CreateTable(
                name: "source_platforms",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "ИД сущности.")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Код платформы (уникальный)."),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "Человекочитаемое название платформы."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by = table.Column<string>(type: "text", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by = table.Column<string>(type: "text", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by = table.Column<string>(type: "text", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_source_platforms", x => x.id);
                },
                comment: "Площадка-источник торгов.");

            migrationBuilder.CreateTable(
                name: "stat_events",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "ИД сущности.")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "Тип события"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by = table.Column<string>(type: "text", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by = table.Column<string>(type: "text", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by = table.Column<string>(type: "text", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stat_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "auctions",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "ИД сущности.")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    domain_id = table.Column<long>(type: "bigint", nullable: false, comment: "Идентификатор домена."),
                    source_platform_id = table.Column<long>(type: "bigint", nullable: false, comment: "Идентификатор площадки."),
                    external_auction_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "Внешний идентификатор лота на площадке."),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Текущий нормализованный статус торгов."),
                    current_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true, comment: "Текущая цена."),
                    final_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true, comment: "Финальная цена."),
                    currency_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true, comment: "Код валюты."),
                    start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Старт торгов."),
                    end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Плановое завершение торгов."),
                    extended_end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Продленное завершение торгов."),
                    is_extended = table.Column<bool>(type: "boolean", nullable: true, comment: "Торги продлены."),
                    terminal_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Финальный terminal статус."),
                    finalized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Время финализации."),
                    lot_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true, comment: "Ссылка на лот."),
                    last_observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Последнее обработанное время наблюдения."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by = table.Column<string>(type: "text", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by = table.Column<string>(type: "text", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by = table.Column<string>(type: "text", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auctions", x => x.id);
                    table.ForeignKey(
                        name: "fk_auctions_domains_domain_id",
                        column: x => x.domain_id,
                        principalSchema: "app",
                        principalTable: "domains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_auctions_source_platforms_source_platform_id",
                        column: x => x.source_platform_id,
                        principalSchema: "app",
                        principalTable: "source_platforms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Торги домена на конкретной площадке.");

            migrationBuilder.CreateTable(
                name: "auction_observations",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "ИД сущности.")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    auction_id = table.Column<long>(type: "bigint", nullable: false, comment: "Связанные торги."),
                    message_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Уникальный идентификатор сообщения."),
                    schema_version = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, comment: "Версия схемы сообщения."),
                    parser_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Код парсера/producer."),
                    parser_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Версия парсера/producer."),
                    observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Время фактического наблюдения."),
                    auction_status_raw = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "Сырой статус источника."),
                    auction_status_normalized = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Нормализованный статус."),
                    current_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true, comment: "Наблюдаемая цена."),
                    final_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true, comment: "Наблюдаемая финальная цена."),
                    currency_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true, comment: "Код валюты."),
                    raw_payload = table.Column<string>(type: "jsonb", nullable: true, comment: "Сырой payload источника (json/string)."),
                    attributes_json = table.Column<string>(type: "jsonb", nullable: true, comment: "Расширяемые атрибуты (json/string)."),
                    is_out_of_order = table.Column<bool>(type: "boolean", nullable: false, comment: "Признак out-of-order события."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by = table.Column<string>(type: "text", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by = table.Column<string>(type: "text", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by = table.Column<string>(type: "text", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_observations", x => x.id);
                    table.ForeignKey(
                        name: "fk_auction_observations_auctions_auction_id",
                        column: x => x.auction_id,
                        principalSchema: "app",
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "История observation-сообщений по торгам.");

            migrationBuilder.CreateTable(
                name: "auction_status_history",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "ИД сущности.")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    auction_id = table.Column<long>(type: "bigint", nullable: false, comment: "Связанные торги."),
                    previous_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Статус до изменения."),
                    new_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Новый статус."),
                    is_terminal = table.Column<bool>(type: "boolean", nullable: false, comment: "Признак terminal статуса."),
                    observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Время наблюдения статуса."),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Источник изменения статуса."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Когда сущность была создана."),
                    created_by = table.Column<string>(type: "text", nullable: true, comment: "Кто создал сущность."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была в последний раз обновлена."),
                    updated_by = table.Column<string>(type: "text", nullable: true, comment: "Кто обновил сущность."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Когда сущность была удалена."),
                    deleted_by = table.Column<string>(type: "text", nullable: true, comment: "Кто удалил сущность.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_auction_status_history_auctions_auction_id",
                        column: x => x.auction_id,
                        principalSchema: "app",
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "История смены статусов торгов.");

            migrationBuilder.InsertData(
                schema: "app",
                table: "source_platforms",
                columns: new[] { "id", "code", "created_at", "created_by", "deleted_at", "deleted_by", "name", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { 1L, "nicksell", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "NickSell", null, null },
                    { 2L, "godaddy", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "GoDaddy", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_auction_observations_auction_id_observed_at",
                schema: "app",
                table: "auction_observations",
                columns: new[] { "auction_id", "observed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_auction_observations_message_id",
                schema: "app",
                table: "auction_observations",
                column: "message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auction_observations_observed_at",
                schema: "app",
                table: "auction_observations",
                column: "observed_at");

            migrationBuilder.CreateIndex(
                name: "IX_auction_status_history_auction_id_new_status",
                schema: "app",
                table: "auction_status_history",
                columns: new[] { "auction_id", "new_status" });

            migrationBuilder.CreateIndex(
                name: "IX_auction_status_history_auction_id_observed_at",
                schema: "app",
                table: "auction_status_history",
                columns: new[] { "auction_id", "observed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_auctions_domain_id",
                schema: "app",
                table: "auctions",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "IX_auctions_end_at",
                schema: "app",
                table: "auctions",
                column: "end_at");

            migrationBuilder.CreateIndex(
                name: "IX_auctions_last_observed_at",
                schema: "app",
                table: "auctions",
                column: "last_observed_at");

            migrationBuilder.CreateIndex(
                name: "IX_auctions_source_platform_id_external_auction_id",
                schema: "app",
                table: "auctions",
                columns: new[] { "source_platform_id", "external_auction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auctions_status",
                schema: "app",
                table: "auctions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_domains_name_normalized",
                schema: "app",
                table: "domains",
                column: "name_normalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_domains_name_punycode",
                schema: "app",
                table: "domains",
                column: "name_punycode");

            migrationBuilder.CreateIndex(
                name: "IX_receiver_processing_logs_message_id",
                schema: "app",
                table: "receiver_processing_logs",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "IX_receiver_processing_logs_processed_at",
                schema: "app",
                table: "receiver_processing_logs",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "IX_receiver_processing_logs_source_platform_code_external_auct~",
                schema: "app",
                table: "receiver_processing_logs",
                columns: new[] { "source_platform_code", "external_auction_id" });

            migrationBuilder.CreateIndex(
                name: "IX_source_platforms_code",
                schema: "app",
                table: "source_platforms",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auction_observations",
                schema: "app");

            migrationBuilder.DropTable(
                name: "auction_status_history",
                schema: "app");

            migrationBuilder.DropTable(
                name: "receiver_processing_logs",
                schema: "app");

            migrationBuilder.DropTable(
                name: "stat_events",
                schema: "app");

            migrationBuilder.DropTable(
                name: "auctions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "domains",
                schema: "app");

            migrationBuilder.DropTable(
                name: "source_platforms",
                schema: "app");
        }
    }
}
