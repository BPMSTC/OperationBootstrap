<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('referrals', function (Blueprint $table) {
            $table->id(); // BIGINT UNSIGNED AUTO_INCREMENT

            $table->unsignedBigInteger('client_user_id');
            $table->unsignedBigInteger('referring_organization_id');

            $table->date('referred_on');
            $table->string('status', 50);

            $table->date('valid_from')->nullable();
            $table->date('valid_to')->nullable();

            $table->string('referred_by_name', 255)->nullable();
            $table->string('referred_by_phone_number', 50)->nullable();
            $table->string('referred_by_email', 255)->nullable();

            $table->text('notes')->nullable();

            // Audit
            $table->unsignedBigInteger('created_by_user_id')->nullable();
            $table->unsignedBigInteger('updated_by_user_id')->nullable();

            // Laravel standard timestamps + soft deletes
            $table->timestamps();
            $table->softDeletes();

            // Indexes from your SQL
            $table->index('client_user_id', 'idx_referrals_client_user_id');
            $table->index('referring_organization_id', 'idx_referrals_referring_org_id');
            $table->index('referred_on', 'idx_referrals_referred_on');
            $table->index(['client_user_id', 'referred_on'], 'idx_referrals_client_user_referred_on');
        });

        // Add FKs after table exists (matches your FK names + delete behaviors)
        Schema::table('referrals', function (Blueprint $table) {
            $table->foreign('client_user_id', 'referrals_client_user_fk')
                ->references('id')->on('users')
                ->restrictOnDelete();

            $table->foreign('referring_organization_id', 'referrals_referring_org_fk')
                ->references('id')->on('referring_organizations')
                ->restrictOnDelete();

            $table->foreign('created_by_user_id', 'referrals_created_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();

            $table->foreign('updated_by_user_id', 'referrals_updated_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();
        });
    }

    public function down(): void
    {
        Schema::table('referrals', function (Blueprint $table) {
            $table->dropForeign('referrals_client_user_fk');
            $table->dropForeign('referrals_referring_org_fk');
            $table->dropForeign('referrals_created_by_fk');
            $table->dropForeign('referrals_updated_by_fk');
        });

        Schema::dropIfExists('referrals');
    }
};
