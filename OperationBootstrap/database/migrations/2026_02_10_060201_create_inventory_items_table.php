<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('inventory_items', function (Blueprint $table) {
            $table->id(); // BIGINT UNSIGNED AUTO_INCREMENT

            $table->string('name', 255);
            $table->unsignedBigInteger('category_id');

            $table->boolean('is_baseline')->default(false);
            $table->boolean('is_available')->default(true);
            $table->boolean('is_active')->default(true);

            // Audit
            $table->unsignedBigInteger('created_by_user_id')->nullable();
            $table->unsignedBigInteger('updated_by_user_id')->nullable();

            // Laravel standard timestamps + soft deletes
            $table->timestamps();
            $table->softDeletes();

            // Indexes from your SQL
            $table->index('category_id', 'idx_inventory_items_category_id');
            $table->index('is_available', 'idx_inventory_items_is_available');
            $table->index('is_active', 'idx_inventory_items_is_active');
        });

        // Add FKs after table exists (matches your FK names + delete behaviors)
        Schema::table('inventory_items', function (Blueprint $table) {
            $table->foreign('category_id', 'inventory_items_category_fk')
                ->references('id')->on('categories')
                ->restrictOnDelete();

            $table->foreign('created_by_user_id', 'inventory_items_created_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();

            $table->foreign('updated_by_user_id', 'inventory_items_updated_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();
        });
    }

    public function down(): void
    {
        Schema::table('inventory_items', function (Blueprint $table) {
            $table->dropForeign('inventory_items_category_fk');
            $table->dropForeign('inventory_items_created_by_fk');
            $table->dropForeign('inventory_items_updated_by_fk');
        });

        Schema::dropIfExists('inventory_items');
    }
};
