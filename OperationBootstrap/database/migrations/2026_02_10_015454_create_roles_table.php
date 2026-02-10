<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('roles', function (Blueprint $table) {
            $table->id();

            $table->string('name', 50)->unique('roles_name_unique');
            $table->text('description')->nullable();

            $table->unsignedBigInteger('created_by_user_id')->nullable();
            $table->unsignedBigInteger('updated_by_user_id')->nullable();

            $table->timestamps();
            $table->softDeletes();

            $table->index('name', 'idx_roles_name');
        });

        Schema::table('roles', function (Blueprint $table) {
            $table->foreign('created_by_user_id', 'roles_created_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();

            $table->foreign('updated_by_user_id', 'roles_updated_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();
        });
    }

    public function down(): void
    {
        Schema::table('roles', function (Blueprint $table) {
            $table->dropForeign('roles_created_by_fk');
            $table->dropForeign('roles_updated_by_fk');
        });

        Schema::dropIfExists('roles');
    }
};
