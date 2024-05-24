#ifndef CILIUM_BINDINGS
#define CILIUM_BINDINGS

#include <stdarg.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdlib.h>

enum AssemblyHashAlgorithm {
    None = 0,
    MD5 = 32771,
    SHA1 = 32772,
    SHA256 = 32780,
    SHA384 = 32781,
    SHA512 = 32782,
};
typedef uint32_t AssemblyHashAlgorithm;

typedef struct BlobHeap BlobHeap;

typedef struct Context Context;

typedef struct GuidHeap GuidHeap;

typedef struct StringHeap StringHeap;

typedef struct UserStringHeap UserStringHeap;

typedef struct Vec_Section Vec_Section;

typedef struct AssemblyFlags {
    Internal _0;
} AssemblyFlags;

typedef uintptr_t BlobIndex;

typedef uintptr_t StringIndex;

typedef struct Assembly {
    AssemblyHashAlgorithm hash_algorithm;
    uint16_t major_version;
    uint16_t minor_version;
    uint16_t build_number;
    uint16_t revision_number;
    struct AssemblyFlags flags;
    BlobIndex public_key;
    StringIndex name;
    StringIndex culture;
} Assembly;

typedef struct IndexSizes {
    uintptr_t guid;
    uintptr_t blob;
    uintptr_t string;
    uintptr_t coded[14];
    uintptr_t tables[55];
} IndexSizes;

typedef struct ModuleTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} ModuleTable;

typedef uintptr_t GuidIndex;

typedef struct Module {
    uint16_t generation;
    StringIndex name;
    GuidIndex mv_id;
    GuidIndex enc_id;
    GuidIndex enc_base_id;
} Module;

typedef struct TypeRefTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} TypeRefTable;

typedef uint32_t ResolutionScope;

typedef struct TypeRef {
    ResolutionScope resolution_scope;
    StringIndex name;
    StringIndex namespace_;
} TypeRef;

typedef struct TypeDefTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} TypeDefTable;

typedef struct TypeAttributes {
    Internal _0;
} TypeAttributes;

typedef uint32_t TypeDefOrRef;

typedef uintptr_t FieldIndex;

typedef uintptr_t MethodDefIndex;

typedef struct TypeDef {
    struct TypeAttributes flags;
    StringIndex name;
    StringIndex namespace_;
    TypeDefOrRef extends;
    FieldIndex field_list;
    MethodDefIndex method_list;
} TypeDef;

typedef struct FieldTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} FieldTable;

typedef struct FieldAttributes {
    Internal _0;
} FieldAttributes;

typedef struct Field {
    struct FieldAttributes flags;
    StringIndex name;
    BlobIndex signature;
} Field;

typedef struct MethodDefTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} MethodDefTable;

typedef struct MethodAttributes {
    Internal _0;
} MethodAttributes;

typedef uintptr_t ParamIndex;

typedef struct MethodDef {
    uint32_t rva;
    struct MethodAttributes impl_flags;
    struct MethodAttributes flags;
    StringIndex name;
    BlobIndex signature;
    ParamIndex param_list;
} MethodDef;

typedef struct ParamTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} ParamTable;

typedef struct ParamAttributes {
    Internal _0;
} ParamAttributes;

typedef struct Param {
    struct ParamAttributes flags;
    uint16_t sequence;
    StringIndex name;
} Param;

typedef struct InterfaceImplTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} InterfaceImplTable;

typedef uintptr_t TypeDefIndex;

typedef struct InterfaceImpl {
    TypeDefIndex class_;
    TypeDefOrRef interface;
} InterfaceImpl;

typedef struct MemberRefTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} MemberRefTable;

typedef uint32_t MemberRefParent;

typedef struct MemberRef {
    MemberRefParent class_;
    StringIndex name;
    BlobIndex signature;
} MemberRef;

typedef struct ConstantTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} ConstantTable;

typedef uint32_t HasConstant;

typedef struct Constant {
    uint8_t ty[2];
    HasConstant parent;
    BlobIndex value;
} Constant;

typedef struct CustomAttributeTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} CustomAttributeTable;

typedef uint32_t HasCustomAttribute;

typedef uint32_t CustomAttributeType;

typedef struct CustomAttribute {
    HasCustomAttribute parent;
    CustomAttributeType ty;
    BlobIndex value;
} CustomAttribute;

typedef struct FieldMarshalTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} FieldMarshalTable;

typedef uint32_t HasFieldMarshal;

typedef struct FieldMarshal {
    HasFieldMarshal parent;
    BlobIndex native_type;
} FieldMarshal;

typedef struct DeclSecurityTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} DeclSecurityTable;

typedef uint32_t HasDeclSecurity;

typedef struct DeclSecurity {
    uint16_t action;
    HasDeclSecurity parent;
    BlobIndex permission_set;
} DeclSecurity;

typedef struct ClassLayoutTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} ClassLayoutTable;

typedef struct ClassLayout {
    uint16_t packing_size;
    uint32_t class_size;
    TypeDefIndex parent;
} ClassLayout;

typedef struct FieldLayoutTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} FieldLayoutTable;

typedef struct FieldLayout {
    uint32_t offset;
    FieldIndex field;
} FieldLayout;

typedef struct StandAloneSigTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} StandAloneSigTable;

typedef struct StandAloneSig {
    BlobIndex signature;
} StandAloneSig;

typedef struct EventMapTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} EventMapTable;

typedef uintptr_t EventIndex;

typedef struct EventMap {
    TypeDefIndex parent;
    EventIndex event_list;
} EventMap;

typedef struct EventTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} EventTable;

typedef struct EventAttributes {
    Internal _0;
} EventAttributes;

typedef struct Event {
    struct EventAttributes flags;
    StringIndex name;
    TypeDefOrRef ty;
} Event;

typedef struct PropertyMapTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} PropertyMapTable;

typedef uintptr_t PropertyIndex;

typedef struct PropertyMap {
    TypeDefIndex parent;
    PropertyIndex property_list;
} PropertyMap;

typedef struct PropertyTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} PropertyTable;

typedef struct PropertyAttributes {
    Internal _0;
} PropertyAttributes;

typedef struct Property {
    struct PropertyAttributes flags;
    StringIndex name;
    BlobIndex ty;
} Property;

typedef struct MethodSemanticsTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} MethodSemanticsTable;

typedef struct MethodSemanticsAttributes {
    Internal _0;
} MethodSemanticsAttributes;

typedef uint32_t HasSemantics;

typedef struct MethodSemantics {
    struct MethodSemanticsAttributes flags;
    MethodDefIndex method;
    HasSemantics association;
} MethodSemantics;

typedef struct MethodImplTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} MethodImplTable;

typedef uint32_t MethodDefOrRef;

typedef struct MethodImpl {
    TypeDefIndex class_;
    MethodDefOrRef body;
    MethodDefOrRef declaration;
} MethodImpl;

typedef struct ModuleRefTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} ModuleRefTable;

typedef struct ModuleRef {
    StringIndex name;
} ModuleRef;

typedef struct TypeSpecTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} TypeSpecTable;

typedef struct TypeSpec {
    BlobIndex signature;
} TypeSpec;

typedef struct ImplMapTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} ImplMapTable;

typedef struct PInvokeAttributes {
    Internal _0;
} PInvokeAttributes;

typedef uint32_t MemberForwarded;

typedef uintptr_t ModuleRefIndex;

typedef struct ImplMap {
    struct PInvokeAttributes flags;
    MemberForwarded member_forwarded;
    StringIndex import_name;
    ModuleRefIndex import_scope;
} ImplMap;

typedef struct FieldRVATable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} FieldRVATable;

typedef struct FieldRVA {
    uint32_t rva;
    FieldIndex field;
} FieldRVA;

typedef struct AssemblyTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} AssemblyTable;

typedef struct AssemblyRefTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} AssemblyRefTable;

typedef struct AssemblyRef {
    uint16_t major_version;
    uint16_t minor_version;
    uint16_t build_number;
    uint16_t revision_number;
    struct AssemblyFlags flags;
    BlobIndex public_key;
    StringIndex name;
    StringIndex culture;
    BlobIndex hash_value;
} AssemblyRef;

typedef struct FileTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} FileTable;

typedef struct FileAttributes {
    Internal _0;
} FileAttributes;

typedef struct File {
    struct FileAttributes flags;
    StringIndex name;
    BlobIndex hash_value;
} File;

typedef struct ExportedTypeTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} ExportedTypeTable;

typedef uint32_t Implementation;

typedef struct ExportedType {
    struct TypeAttributes flags;
    TypeDefIndex type_def;
    StringIndex name;
    StringIndex namespace_;
    Implementation implementation;
} ExportedType;

typedef struct ManifestResourceTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} ManifestResourceTable;

typedef struct ManifestResourceAttributes {
    Internal _0;
} ManifestResourceAttributes;

typedef struct ManifestResource {
    uint32_t offset;
    struct ManifestResourceAttributes flags;
    StringIndex name;
    Implementation implementation;
} ManifestResource;

typedef struct NestedClassTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} NestedClassTable;

typedef struct NestedClass {
    TypeDefIndex nested_class;
    TypeDefIndex enclosing_class;
} NestedClass;

typedef struct GenericParamTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} GenericParamTable;

typedef struct GenericParamAttributes {
    Internal _0;
} GenericParamAttributes;

typedef uint32_t TypeOrMethodDef;

typedef struct GenericParam {
    uint16_t number;
    struct GenericParamAttributes flags;
    TypeOrMethodDef owner;
    StringIndex name;
} GenericParam;

typedef struct MethodSpecTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} MethodSpecTable;

typedef struct MethodSpec {
    MethodDefOrRef method;
    BlobIndex instantiation;
} MethodSpec;

typedef struct GenericParamConstraintTable {
    uintptr_t len;
    uintptr_t row_size;
    const uint8_t *data;
    uintptr_t data_len;
    const struct IndexSizes *idx_sizes;
} GenericParamConstraintTable;

typedef uintptr_t GenericParamIndex;

typedef struct GenericParamConstraint {
    GenericParamIndex owner;
    TypeDefOrRef constraint;
} GenericParamConstraint;

typedef struct DOSHeader {
    uint16_t magic;
    uint16_t last_page_bytes;
    uint16_t file_pages;
    uint16_t relocations;
    uint16_t header_size;
    uint16_t min_alloc;
    uint16_t max_alloc;
    uint16_t ss;
    uint16_t sp;
    uint16_t checksum;
    uint16_t ip;
    uint16_t cs;
    uint16_t relocation_table_address;
    uint16_t overlay_number;
    uint16_t reserved[4];
    uint16_t oem_id;
    uint16_t oem_info;
    uint16_t reserved_2[10];
    uint32_t new_header_start;
} DOSHeader;

typedef struct ImageFileHeader {
    uint16_t machine;
    uint16_t number_of_sections;
    uint32_t time_date_stamp;
    uint32_t pointer_to_symbol_table;
    uint32_t number_of_symbols;
    uint16_t size_of_optional_header;
    uint16_t characteristics;
} ImageFileHeader;

typedef struct DataDirectory {
    uint32_t virtual_address;
    uint32_t size;
} DataDirectory;

typedef struct ImageOptionalHeader32 {
    uint16_t magic;
    uint8_t major_linker_version;
    uint8_t minor_linker_version;
    uint32_t size_of_code;
    uint32_t size_of_initialized_data;
    uint32_t size_of_uninitialized_data;
    uint32_t address_of_entry_point;
    uint32_t base_of_code;
    uint32_t base_of_data;
    uint32_t image_base;
    uint32_t section_alignment;
    uint32_t file_alignment;
    uint16_t major_operating_system_version;
    uint16_t minor_operating_system_version;
    uint16_t major_image_version;
    uint16_t minor_image_version;
    uint16_t major_subsystem_version;
    uint16_t minor_subsystem_version;
    uint32_t win32_version_value;
    uint32_t size_of_image;
    uint32_t size_of_headers;
    uint32_t check_sum;
    uint16_t subsystem;
    uint16_t dll_characteristics;
    uint32_t size_of_stack_reserve;
    uint32_t size_of_stack_commit;
    uint32_t size_of_heap_reserve;
    uint32_t size_of_heap_commit;
    uint32_t loader_flags;
    uint32_t number_of_rva_and_sizes;
    struct DataDirectory data_directories[16];
} ImageOptionalHeader32;

typedef struct ImageOptionalHeader64 {
    uint16_t magic;
    uint8_t major_linker_version;
    uint8_t minor_linker_version;
    uint32_t size_of_code;
    uint32_t size_of_initialized_data;
    uint32_t size_of_uninitialized_data;
    uint32_t address_of_entry_point;
    uint32_t base_of_code;
    uint64_t image_base;
    uint32_t section_alignment;
    uint32_t file_alignment;
    uint16_t major_operating_system_version;
    uint16_t minor_operating_system_version;
    uint16_t major_image_version;
    uint16_t minor_image_version;
    uint16_t major_subsystem_version;
    uint16_t minor_subsystem_version;
    uint32_t win32_version_value;
    uint32_t size_of_image;
    uint32_t size_of_headers;
    uint32_t check_sum;
    uint16_t subsystem;
    uint16_t dll_characteristics;
    uint64_t size_of_stack_reserve;
    uint64_t size_of_stack_commit;
    uint64_t size_of_heap_reserve;
    uint64_t size_of_heap_commit;
    uint32_t loader_flags;
    uint32_t number_of_rva_and_sizes;
    struct DataDirectory data_directories[16];
} ImageOptionalHeader64;

enum ImageOptionalHeader_Tag {
    None = 0,
    PE32 = 267,
    PE64 = 523,
};
typedef uint16_t ImageOptionalHeader_Tag;

typedef struct ImageOptionalHeader {
    ImageOptionalHeader_Tag tag;
    union {
        struct {
            struct ImageOptionalHeader32 pe32;
        };
        struct {
            struct ImageOptionalHeader64 pe64;
        };
    };
} ImageOptionalHeader;

typedef struct PEHeader {
    uint32_t magic;
    struct ImageFileHeader image_file_header;
    struct ImageOptionalHeader image_optional_header;
} PEHeader;

typedef struct PEFile {
    struct DOSHeader dos_header;
    struct PEHeader pe_header;
    struct Vec_Section sections;
} PEFile;

typedef struct RuntimeFlags {
    Internal _0;
} RuntimeFlags;

typedef uint32_t MetadataToken;

typedef struct CLIHeader {
    uint32_t size_in_bytes;
    uint16_t major_runtime_version;
    uint16_t minot_runtime_version;
    struct DataDirectory metadata;
    struct RuntimeFlags flags;
    MetadataToken entry_point_token;
    struct DataDirectory resources;
    uint64_t strong_name_signature;
    uint64_t code_manager_table;
    uint64_t v_table_fixups;
    uint64_t export_address_table_jumps;
    uint64_t managed_native_header;
} CLIHeader;

bool cilium_raw_Assembly_get_table_Module(const struct Assembly *assembly,
                                          struct ModuleTable *out_table);

bool cilium_raw_ModuleTable_get_row(const struct ModuleTable *table,
                                    uintptr_t idx,
                                    struct Module *out_row);

void cilium_raw_ModuleTable_destroy(struct ModuleTable *table);

bool cilium_raw_Assembly_get_table_TypeRef(const struct Assembly *assembly,
                                           struct TypeRefTable *out_table);

bool cilium_raw_TypeRefTable_get_row(const struct TypeRefTable *table,
                                     uintptr_t idx,
                                     struct TypeRef *out_row);

void cilium_raw_TypeRefTable_destroy(struct TypeRefTable *table);

bool cilium_raw_Assembly_get_table_TypeDef(const struct Assembly *assembly,
                                           struct TypeDefTable *out_table);

bool cilium_raw_TypeDefTable_get_row(const struct TypeDefTable *table,
                                     uintptr_t idx,
                                     struct TypeDef *out_row);

void cilium_raw_TypeDefTable_destroy(struct TypeDefTable *table);

bool cilium_raw_Assembly_get_table_Field(const struct Assembly *assembly,
                                         struct FieldTable *out_table);

bool cilium_raw_FieldTable_get_row(const struct FieldTable *table,
                                   uintptr_t idx,
                                   struct Field *out_row);

void cilium_raw_FieldTable_destroy(struct FieldTable *table);

bool cilium_raw_Assembly_get_table_MethodDef(const struct Assembly *assembly,
                                             struct MethodDefTable *out_table);

bool cilium_raw_MethodDefTable_get_row(const struct MethodDefTable *table,
                                       uintptr_t idx,
                                       struct MethodDef *out_row);

void cilium_raw_MethodDefTable_destroy(struct MethodDefTable *table);

bool cilium_raw_Assembly_get_table_Param(const struct Assembly *assembly,
                                         struct ParamTable *out_table);

bool cilium_raw_ParamTable_get_row(const struct ParamTable *table,
                                   uintptr_t idx,
                                   struct Param *out_row);

void cilium_raw_ParamTable_destroy(struct ParamTable *table);

bool cilium_raw_Assembly_get_table_InterfaceImpl(const struct Assembly *assembly,
                                                 struct InterfaceImplTable *out_table);

bool cilium_raw_InterfaceImplTable_get_row(const struct InterfaceImplTable *table,
                                           uintptr_t idx,
                                           struct InterfaceImpl *out_row);

void cilium_raw_InterfaceImplTable_destroy(struct InterfaceImplTable *table);

bool cilium_raw_Assembly_get_table_MemberRef(const struct Assembly *assembly,
                                             struct MemberRefTable *out_table);

bool cilium_raw_MemberRefTable_get_row(const struct MemberRefTable *table,
                                       uintptr_t idx,
                                       struct MemberRef *out_row);

void cilium_raw_MemberRefTable_destroy(struct MemberRefTable *table);

bool cilium_raw_Assembly_get_table_Constant(const struct Assembly *assembly,
                                            struct ConstantTable *out_table);

bool cilium_raw_ConstantTable_get_row(const struct ConstantTable *table,
                                      uintptr_t idx,
                                      struct Constant *out_row);

void cilium_raw_ConstantTable_destroy(struct ConstantTable *table);

bool cilium_raw_Assembly_get_table_CustomAttribute(const struct Assembly *assembly,
                                                   struct CustomAttributeTable *out_table);

bool cilium_raw_CustomAttributeTable_get_row(const struct CustomAttributeTable *table,
                                             uintptr_t idx,
                                             struct CustomAttribute *out_row);

void cilium_raw_CustomAttributeTable_destroy(struct CustomAttributeTable *table);

bool cilium_raw_Assembly_get_table_FieldMarshal(const struct Assembly *assembly,
                                                struct FieldMarshalTable *out_table);

bool cilium_raw_FieldMarshalTable_get_row(const struct FieldMarshalTable *table,
                                          uintptr_t idx,
                                          struct FieldMarshal *out_row);

void cilium_raw_FieldMarshalTable_destroy(struct FieldMarshalTable *table);

bool cilium_raw_Assembly_get_table_DeclSecurity(const struct Assembly *assembly,
                                                struct DeclSecurityTable *out_table);

bool cilium_raw_DeclSecurityTable_get_row(const struct DeclSecurityTable *table,
                                          uintptr_t idx,
                                          struct DeclSecurity *out_row);

void cilium_raw_DeclSecurityTable_destroy(struct DeclSecurityTable *table);

bool cilium_raw_Assembly_get_table_ClassLayout(const struct Assembly *assembly,
                                               struct ClassLayoutTable *out_table);

bool cilium_raw_ClassLayoutTable_get_row(const struct ClassLayoutTable *table,
                                         uintptr_t idx,
                                         struct ClassLayout *out_row);

void cilium_raw_ClassLayoutTable_destroy(struct ClassLayoutTable *table);

bool cilium_raw_Assembly_get_table_FieldLayout(const struct Assembly *assembly,
                                               struct FieldLayoutTable *out_table);

bool cilium_raw_FieldLayoutTable_get_row(const struct FieldLayoutTable *table,
                                         uintptr_t idx,
                                         struct FieldLayout *out_row);

void cilium_raw_FieldLayoutTable_destroy(struct FieldLayoutTable *table);

bool cilium_raw_Assembly_get_table_StandAloneSig(const struct Assembly *assembly,
                                                 struct StandAloneSigTable *out_table);

bool cilium_raw_StandAloneSigTable_get_row(const struct StandAloneSigTable *table,
                                           uintptr_t idx,
                                           struct StandAloneSig *out_row);

void cilium_raw_StandAloneSigTable_destroy(struct StandAloneSigTable *table);

bool cilium_raw_Assembly_get_table_EventMap(const struct Assembly *assembly,
                                            struct EventMapTable *out_table);

bool cilium_raw_EventMapTable_get_row(const struct EventMapTable *table,
                                      uintptr_t idx,
                                      struct EventMap *out_row);

void cilium_raw_EventMapTable_destroy(struct EventMapTable *table);

bool cilium_raw_Assembly_get_table_Event(const struct Assembly *assembly,
                                         struct EventTable *out_table);

bool cilium_raw_EventTable_get_row(const struct EventTable *table,
                                   uintptr_t idx,
                                   struct Event *out_row);

void cilium_raw_EventTable_destroy(struct EventTable *table);

bool cilium_raw_Assembly_get_table_PropertyMap(const struct Assembly *assembly,
                                               struct PropertyMapTable *out_table);

bool cilium_raw_PropertyMapTable_get_row(const struct PropertyMapTable *table,
                                         uintptr_t idx,
                                         struct PropertyMap *out_row);

void cilium_raw_PropertyMapTable_destroy(struct PropertyMapTable *table);

bool cilium_raw_Assembly_get_table_Property(const struct Assembly *assembly,
                                            struct PropertyTable *out_table);

bool cilium_raw_PropertyTable_get_row(const struct PropertyTable *table,
                                      uintptr_t idx,
                                      struct Property *out_row);

void cilium_raw_PropertyTable_destroy(struct PropertyTable *table);

bool cilium_raw_Assembly_get_table_MethodSemantics(const struct Assembly *assembly,
                                                   struct MethodSemanticsTable *out_table);

bool cilium_raw_MethodSemanticsTable_get_row(const struct MethodSemanticsTable *table,
                                             uintptr_t idx,
                                             struct MethodSemantics *out_row);

void cilium_raw_MethodSemanticsTable_destroy(struct MethodSemanticsTable *table);

bool cilium_raw_Assembly_get_table_MethodImpl(const struct Assembly *assembly,
                                              struct MethodImplTable *out_table);

bool cilium_raw_MethodImplTable_get_row(const struct MethodImplTable *table,
                                        uintptr_t idx,
                                        struct MethodImpl *out_row);

void cilium_raw_MethodImplTable_destroy(struct MethodImplTable *table);

bool cilium_raw_Assembly_get_table_ModuleRef(const struct Assembly *assembly,
                                             struct ModuleRefTable *out_table);

bool cilium_raw_ModuleRefTable_get_row(const struct ModuleRefTable *table,
                                       uintptr_t idx,
                                       struct ModuleRef *out_row);

void cilium_raw_ModuleRefTable_destroy(struct ModuleRefTable *table);

bool cilium_raw_Assembly_get_table_TypeSpec(const struct Assembly *assembly,
                                            struct TypeSpecTable *out_table);

bool cilium_raw_TypeSpecTable_get_row(const struct TypeSpecTable *table,
                                      uintptr_t idx,
                                      struct TypeSpec *out_row);

void cilium_raw_TypeSpecTable_destroy(struct TypeSpecTable *table);

bool cilium_raw_Assembly_get_table_ImplMap(const struct Assembly *assembly,
                                           struct ImplMapTable *out_table);

bool cilium_raw_ImplMapTable_get_row(const struct ImplMapTable *table,
                                     uintptr_t idx,
                                     struct ImplMap *out_row);

void cilium_raw_ImplMapTable_destroy(struct ImplMapTable *table);

bool cilium_raw_Assembly_get_table_FieldRVA(const struct Assembly *assembly,
                                            struct FieldRVATable *out_table);

bool cilium_raw_FieldRVATable_get_row(const struct FieldRVATable *table,
                                      uintptr_t idx,
                                      struct FieldRVA *out_row);

void cilium_raw_FieldRVATable_destroy(struct FieldRVATable *table);

bool cilium_raw_Assembly_get_table_Assembly(const struct Assembly *assembly,
                                            struct AssemblyTable *out_table);

bool cilium_raw_AssemblyTable_get_row(const struct AssemblyTable *table,
                                      uintptr_t idx,
                                      struct Assembly *out_row);

void cilium_raw_AssemblyTable_destroy(struct AssemblyTable *table);

bool cilium_raw_Assembly_get_table_AssemblyRef(const struct Assembly *assembly,
                                               struct AssemblyRefTable *out_table);

bool cilium_raw_AssemblyRefTable_get_row(const struct AssemblyRefTable *table,
                                         uintptr_t idx,
                                         struct AssemblyRef *out_row);

void cilium_raw_AssemblyRefTable_destroy(struct AssemblyRefTable *table);

bool cilium_raw_Assembly_get_table_File(const struct Assembly *assembly,
                                        struct FileTable *out_table);

bool cilium_raw_FileTable_get_row(const struct FileTable *table,
                                  uintptr_t idx,
                                  struct File *out_row);

void cilium_raw_FileTable_destroy(struct FileTable *table);

bool cilium_raw_Assembly_get_table_ExportedType(const struct Assembly *assembly,
                                                struct ExportedTypeTable *out_table);

bool cilium_raw_ExportedTypeTable_get_row(const struct ExportedTypeTable *table,
                                          uintptr_t idx,
                                          struct ExportedType *out_row);

void cilium_raw_ExportedTypeTable_destroy(struct ExportedTypeTable *table);

bool cilium_raw_Assembly_get_table_ManifestResource(const struct Assembly *assembly,
                                                    struct ManifestResourceTable *out_table);

bool cilium_raw_ManifestResourceTable_get_row(const struct ManifestResourceTable *table,
                                              uintptr_t idx,
                                              struct ManifestResource *out_row);

void cilium_raw_ManifestResourceTable_destroy(struct ManifestResourceTable *table);

bool cilium_raw_Assembly_get_table_NestedClass(const struct Assembly *assembly,
                                               struct NestedClassTable *out_table);

bool cilium_raw_NestedClassTable_get_row(const struct NestedClassTable *table,
                                         uintptr_t idx,
                                         struct NestedClass *out_row);

void cilium_raw_NestedClassTable_destroy(struct NestedClassTable *table);

bool cilium_raw_Assembly_get_table_GenericParam(const struct Assembly *assembly,
                                                struct GenericParamTable *out_table);

bool cilium_raw_GenericParamTable_get_row(const struct GenericParamTable *table,
                                          uintptr_t idx,
                                          struct GenericParam *out_row);

void cilium_raw_GenericParamTable_destroy(struct GenericParamTable *table);

bool cilium_raw_Assembly_get_table_MethodSpec(const struct Assembly *assembly,
                                              struct MethodSpecTable *out_table);

bool cilium_raw_MethodSpecTable_get_row(const struct MethodSpecTable *table,
                                        uintptr_t idx,
                                        struct MethodSpec *out_row);

void cilium_raw_MethodSpecTable_destroy(struct MethodSpecTable *table);

bool cilium_raw_Assembly_get_table_GenericParamConstraint(const struct Assembly *assembly,
                                                          struct GenericParamConstraintTable *out_table);

bool cilium_raw_GenericParamConstraintTable_get_row(const struct GenericParamConstraintTable *table,
                                                    uintptr_t idx,
                                                    struct GenericParamConstraint *out_row);

void cilium_raw_GenericParamConstraintTable_destroy(struct GenericParamConstraintTable *table);

struct PEFile cilium_raw_PEFile_create(const uint8_t *buffer, uintptr_t len);

void cilium_raw_PEFile_destroy(struct PEFile pe);

struct Assembly *cilium_raw_Assembly_create(struct PEFile pe);

void cilium_raw_Assembly_destroy(struct Assembly *assembly);

struct CLIHeader cilium_raw_Assembly_cli_header(struct Assembly *assembly);

const struct BlobHeap *cilium_raw_Assembly_get_heap_Blob(const struct Assembly *assembly);

const struct GuidHeap *cilium_raw_Assembly_get_heap_Guid(const struct Assembly *assembly);

const struct StringHeap *cilium_raw_Assembly_get_heap_String(const struct Assembly *assembly);

const struct UserStringHeap *cilium_raw_Assembly_get_heap_UserString(const struct Assembly *assembly);

bool cilium_raw_BlobHeap_get(const struct BlobHeap *heap,
                             BlobIndex idx,
                             const uint8_t **out_blob_ptr,
                             uintptr_t *out_blob_len);

bool cilium_raw_GuidHeap_get(const struct GuidHeap *heap, GuidIndex idx, Uuid *out_guid);

bool cilium_raw_StringHeap_get(const struct StringHeap *heap,
                               StringIndex idx,
                               const uint8_t **out_str_ptr,
                               uintptr_t *out_str_len);

struct Context *cilium_Context_create(const char *const *paths, uintptr_t path_count);

void cilium_Context_destroy(struct Context *ctx);

const struct Assembly *cilium_Context_load_assembly(struct Context *ctx, const char *path);

#endif /* CILIUM_BINDINGS */
