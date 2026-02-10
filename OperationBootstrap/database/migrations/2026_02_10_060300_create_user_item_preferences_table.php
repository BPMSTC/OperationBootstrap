<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('user_item_preferences', function (Blueprint $table) {
            $table->id(); // BIGINT UNSIGNED AUTO_INCREMENT

            $table->unsignedBigInteger('user_id');
            $table->unsignedBigInteger('inventory_item_id');
            $table->string('preference', 20);

            $table->unsignedBigInteger('updated_by_user_id')->nullable();

            // Laravel timestamps only (your SQL has created_at/updated_at, no deleted_at)
            $table->timestamps();

            // Unique + indexes from your SQL
            $table->unique(['user_id', 'inventory_item_id'], 'user_item_preferences_unique');
            $table->index('inventory_item_id', 'idx_user_item_preferences_inventory_item_id');
            $table->index('user_id', 'idx_user_item_preferences_user_id');
        });

        // Add FKs after table exists (matches your FK names + delete behaviors)
        Schema::table('user_item_preferences', function (Blueprint $table) {
            $table->foreign('user_id', 'user_item_preferences_user_fk')
                ->references('id')->on('users')
                ->cascadeOnDelete();

            $table->foreign('inventory_item_id', 'user_item_preferences_inventory_item_fk')
                ->references('id')->on('inventory_items')
                ->cascadeOnDelete();

            $table->foreign('updated_by_user_id', 'user_item_preferences_updated_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();
        });
    }

    public function down(): void
    {
        Schema::table('user_item_preferences', function (Blueprint $table) {
            $table->dropForeign('user_item_preferences_user_fk');
            $table->dropForeign('user_item_preferences_inventory_item_fk');
            $table->dropForeign('user_item_preferences_updated_by_fk');
        });

        Schema::dropIfExists('user_item_preferences');
    }
};
