import { Eye, MoreHorizontal, Pen, Trash2 } from "lucide-react";
import ActionButton from "../common/ActionButton";
import { PermissionGate } from "@/components/PermissionGate";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";

interface RowActionsProps {
  row: any;
  onView: (row: any) => void;
  onEdit: (row: any) => void;
  onDelete: (row: any) => void;
  viewPermission?: string;
  editPermission?: string;
  deletePermission?: string;
  compact?: boolean;
}

export function RowActions({
  row,
  onView,
  onEdit,
  onDelete,
  viewPermission,
  editPermission,
  deletePermission,
  compact = false,
}: RowActionsProps) {
  if (compact) {
    return (
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7 p-0 text-muted-foreground hover:text-foreground"
            onClick={(e) => e.stopPropagation()}
          >
            <MoreHorizontal className="h-4 w-4" />
            <span className="sr-only">Actions</span>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" onClick={(e) => e.stopPropagation()}>
          <PermissionGate permission={viewPermission}>
            <DropdownMenuItem
              onClick={(e) => {
                e.stopPropagation();
                onView(row);
              }}
            >
              <Eye className="mr-2 h-4 w-4 text-muted-foreground" />
              <span>View detail</span>
            </DropdownMenuItem>
          </PermissionGate>
          <PermissionGate permission={editPermission}>
            <DropdownMenuItem
              onClick={(e) => {
                e.stopPropagation();
                onEdit(row);
              }}
            >
              <Pen className="mr-2 h-4 w-4 text-muted-foreground" />
              <span>Edit</span>
            </DropdownMenuItem>
          </PermissionGate>
          <PermissionGate permission={deletePermission}>
            <DropdownMenuItem
              onClick={(e) => {
                e.stopPropagation();
                onDelete(row);
              }}
              className="text-destructive focus:text-destructive"
            >
              <Trash2 className="mr-2 h-4 w-4 text-destructive" />
              <span>Delete</span>
            </DropdownMenuItem>
          </PermissionGate>
        </DropdownMenuContent>
      </DropdownMenu>
    );
  }

  return (
    <>
      <PermissionGate permission={viewPermission}>
        <ActionButton
          onClick={() => onView(row)}
          tooltip="View detail"
          className="h-7 w-7 p-0"
          icon={<Eye size={12} className="text-gray-500" />}
        />
      </PermissionGate>
      <PermissionGate permission={editPermission}>
        <ActionButton
          onClick={() => onEdit(row)}
          tooltip="Edit"
          className="h-7 w-7 p-0"
          icon={<Pen size={12} className="text-gray-500" />}
        />
      </PermissionGate>
      <PermissionGate permission={deletePermission}>
        <ActionButton
          onClick={() => onDelete(row)}
          tooltip="Delete"
          className="h-7 w-7 p-0"
          icon={<Trash2 size={12} className="text-red-500" />}
        />
      </PermissionGate>
    </>
  );
}
